// --------------- Bootstrapping ---------------

// Defensive: if ELK didn't load (wrong path / blocked), try to fetch it.
async function ensureElk() {
    if (window.ELK) return new window.ELK();
    // Fallback: dynamically inject the script
    await new Promise((resolve, reject) => {
        const s = document.createElement("script");
        s.src = "https://unpkg.com/elkjs@0.9.3/lib/elk.bundled.js";
        s.onload = resolve;
        s.onerror = reject;
        document.head.appendChild(s);
    }).catch(() => { });
    return window.ELK ? new window.ELK() : null;
}

// Make the whole app async so we can await ELK fallback
(async function main() {
    const STORAGE_KEY = "ifc-graph-state-v3";

    // ---------- Data ----------
    const data = (window && window.GRAPH_DATA) ? window.GRAPH_DATA : { nodes: {}, relations: {} };
    const nodesMap = new Map();
    for (const [id, props] of Object.entries(data.nodes || {})) nodesMap.set(id, { id, ...(props || {}) });

    const links = [];
    function ensureNode(id) { if (!nodesMap.has(id)) nodesMap.set(id, { id }); return nodesMap.get(id); }
    function addRelation(source, rel) {
        const target = rel.name;
        const cnt = +rel.count || 0;
        ensureNode(source); ensureNode(target);
        links.push({ source, target, label: String(cnt), countNum: cnt });
    }
    for (const [src, val] of Object.entries(data.relations || {})) {
        if (Array.isArray(val)) val.forEach(r => addRelation(src, r));
        else if (val && typeof val === "object") addRelation(src, val);
    }
    const nodes = Array.from(nodesMap.values());
    const idOf = x => (typeof x === "string" ? x : x.id);

    // Directed neighbor map (for radial layering)
    const neighborsDir = (() => {
        const m = new Map(); nodes.forEach(n => m.set(n.id, []));
        links.forEach(l => m.get(idOf(l.source)).push(idOf(l.target)));
        return m;
    })();

    // ---------- UI elements ----------
    const svg = d3.select("svg");
    const container = svg.append("g");

    const layoutModeSel = document.getElementById("layoutMode");
    const focusRow = document.getElementById("focusRow");
    const focusNodeSel = document.getElementById("focusNode");
    const sizeByCountSel = document.getElementById("sizeByCount");
    const fitBtn = document.getElementById("fitBtn");
    const freezeBtn = document.getElementById("freezeBtn");
    const unpinBtn = document.getElementById("unpinBtn");

    // Populate focus selector
    const nodeIdsSorted = nodes.map(n => n.id).sort((a, b) => a.localeCompare(b));
    for (const id of nodeIdsSorted) {
        const opt = document.createElement("option");
        opt.value = id; opt.textContent = id;
        focusNodeSel.appendChild(opt);
    }

    // ---------- State ----------
    const maxNodeCount = d3.max(nodes, d => d.count || 0) || 1;
    let layoutMode = "force";
    let radialFocusId = pickDefaultFocus();
    focusNodeSel.value = radialFocusId;
    let sizeByCount = true;
    let frozen = false;

    const rScale = d3.scaleSqrt().domain([0, maxNodeCount]).range([12, 32]);

    // ---------- Zoom (disable dblclick zoom so dblclick pins) ----------
    const zoom = d3.zoom()
        .scaleExtent([0.2, 4])
        .filter((event) => event.type !== "dblclick")
        .on("zoom", e => container.attr("transform", e.transform));
    svg.call(zoom);

    // ---------- Simulation ----------
    const simulation = d3.forceSimulation(nodes)
        .force("link", d3.forceLink(links).id(d => d.id).distance(150))
        .force("charge", d3.forceManyBody().strength(-350))
        .force("center", d3.forceCenter(window.innerWidth / 2, window.innerHeight / 2))
        .force("collision", d3.forceCollide().radius(d => nodeRadius(d) + 4));

    // ---------- Draw ----------
    const linkSel = container.selectAll(".link")
        .data(links)
        .enter().append("line")
        .attr("class", "link")
        .attr("stroke-width", 2);

    const edgeLabelSel = container.selectAll(".edge-label")
        .data(links)
        .enter().append("text")
        .attr("class", "edge-label")
        .attr("dy", -4)
        .text(d => d.label);

    const nodeSel = container.selectAll(".node")
        .data(nodes, d => d.id)
        .enter().append("g")
        .attr("class", "node")
        .on("dblclick", (event, d) => { event.stopPropagation(); toggleUserPin(d); saveState(); })
        .call(makeNodeDrag(simulation));

    const nodeCircle = nodeSel.append("circle")
        .attr("r", d => nodeRadius(d))
        .attr("fill", d => d.color || "steelblue");

    nodeSel.append("text")
        .attr("class", "node-label")
        .attr("dy", "0.1em")
        .text(d => d.id);

    nodeSel.append("text")
        .attr("class", "node-count")
        .attr("dy", "1.5em")
        .text(d => (d.count == null ? "" : `count: ${d.count}`));

    nodeSel.append("text")
        .attr("class", "pin-icon")
        .text("📌");

    simulation.on("tick", ticked);

    function ticked() {
        linkSel
            .attr("x1", d => d.source.x).attr("y1", d => d.source.y)
            .attr("x2", d => d.target.x).attr("y2", d => d.target.y);

        edgeLabelSel
            .attr("x", d => (d.source.x + d.target.x) / 2)
            .attr("y", d => (d.source.y + d.target.y) / 2);

        nodeSel.attr("transform", d => `translate(${d.x},${d.y})`);
        positionPinIcons();
    }

    // ---------- Helpers ----------
    function nodeRadius(d) { return sizeByCount ? (10 + rScale(d.count || 0)) : 22; }
    function isPinned(d) { return d.fx != null && d.fy != null; }
    function positionPinIcons() {
        nodeSel.each(function (d) {
            const r = nodeRadius(d);
            d3.select(this).select(".pin-icon")
                .attr("x", r - 2)
                .attr("y", -r + 10)
                .classed("hidden", !isPinned(d));
        });
    }
    function pickDefaultFocus() {
        if (nodesMap.has("IfcProject")) return "IfcProject";
        if (nodesMap.has("IfcBuilding")) return "IfcBuilding";
        return nodes[0]?.id ?? "";
    }

    // Pinning
    function toggleUserPin(d) {
        if (isPinned(d)) { d.fx = null; d.fy = null; d.pinMode = null; }
        else { d.fx = d.x; d.fy = d.y; d.pinMode = "user"; }
        positionPinIcons();
    }
    function unpinAll() {
        nodes.forEach(n => { n.fx = null; n.fy = null; n.pinMode = null; });
        positionPinIcons();
        if (!frozen) simulation.alpha(0.15).restart();
    }

    // Drag behavior: temporary pin, not persisted
    function makeNodeDrag(sim) {
        function dragstarted(event, d) {
            d._wasPinned = isPinned(d);
            if (!event.active && !frozen) sim.alphaTarget(0.3).restart();
            if (!d._wasPinned) { d.fx = d.x; d.fy = d.y; } // temp
        }
        function dragged(event, d) {
            d.fx = event.x; d.fy = event.y;
            if (frozen) ticked(); // manual repaint while frozen
        }
        function dragended(event, d) {
            if (!event.active && !frozen) sim.alphaTarget(0);
            if (!d._wasPinned) { d.fx = null; d.fy = null; } // release if it wasn't pinned before
            delete d._wasPinned;
            saveState();
        }
        return d3.drag().on("start", dragstarted).on("drag", dragged).on("end", dragended);
    }

    // ---------- Layouts ----------
    let elk = null; // resolved lazily on demand
    async function applyLayout(mode) {
        layoutMode = mode;
        showFocusIfNeeded();

        if (mode === "force") {
            // Clear non-user pins
            nodes.forEach(n => { if (n.pinMode && n.pinMode !== "user") { n.fx = null; n.fy = null; n.pinMode = null; } });
            positionPinIcons();
            if (!frozen) simulation.alpha(0.2).restart();
            return;
        }

        if (mode === "hier") {
            // Ensure ELK is available; if not, fallback to force and disable hier option
            if (!elk) elk = await ensureElk();
            if (!elk) {
                // Graceful fallback
                layoutModeSel.value = "force";
                alert("Hierarchical layout requires ELK (could not load). Falling back to Force.");
                await applyLayout("force");
                // Disable Hier option to avoid repeated alerts
                [...layoutModeSel.options].forEach(o => { if (o.value === "hier") o.disabled = true; });
                return;
            }
            await runHierLayout(elk);
            return;
        }

        if (mode === "radial") {
            runRadialLayout(radialFocusId);
            return;
        }

        if (mode === "circular") {
            runCircularLayout();
            return;
        }
    }

    async function runHierLayout(elkInstance) {
        const elkGraph = {
            id: "root",
            layoutOptions: {
                "elk.algorithm": "layered",
                "elk.direction": "RIGHT",
                "elk.spacing.nodeNode": "30",
                "elk.layered.spacing.nodeNodeBetweenLayers": "60",
                "elk.spacing.edgeEdge": "20"
            },
            children: nodes.map(n => ({ id: n.id, width: 140, height: 40 })),
            edges: links.map((l, i) => ({ id: `e${i}`, sources: [idOf(l.source)], targets: [idOf(l.target)] }))
        };

        const result = await elkInstance.layout(elkGraph);
        const pos = new Map(result.children.map(c => [c.id, c]));
        nodes.forEach(n => {
            const c = pos.get(n.id);
            if (c) {
                const x = c.x + c.width / 2, y = c.y + c.height / 2;
                n.x = x; n.y = y; n.fx = x; n.fy = y; n.pinMode = "layout";
            }
        });
        positionPinIcons();
        ticked();
        if (!frozen) simulation.alpha(0.001).restart();
    }

    function runCircularLayout() {
        const W = svg.node().clientWidth, H = svg.node().clientHeight;
        const cx = W / 2, cy = H / 2;
        const R = Math.max(Math.min(W, H) * 0.38, 150);
        const n = nodes.length;
        nodes.forEach((node, i) => {
            const a = (i / n) * 2 * Math.PI;
            const x = cx + R * Math.cos(a), y = cy + R * Math.sin(a);
            node.x = x; node.y = y; node.fx = x; node.fy = y; node.pinMode = "layout";
        });
        positionPinIcons();
        ticked();
        if (!frozen) simulation.alpha(0.001).restart();
    }

    function runRadialLayout(rootId) {
        // BFS layering from root
        const dist = new Map(nodes.map(n => [n.id, Infinity]));
        const q = [rootId]; dist.set(rootId, 0);
        for (let qi = 0; qi < q.length; qi++) {
            const u = q[qi];
            for (const v of (neighborsDir.get(u) || [])) {
                if (dist.get(v) === Infinity) { dist.set(v, dist.get(u) + 1); q.push(v); }
            }
        }
        const maxDist = [...dist.values()].reduce((a, b) => Math.max(a, (b === Infinity ? 0 : b)), 0);
        const levels = new Map();
        nodes.forEach(n => {
            const d = dist.get(n.id);
            const lvl = (d === Infinity) ? (maxDist + 1) : d;
            if (!levels.has(lvl)) levels.set(lvl, []);
            levels.get(lvl).push(n);
        });

        const W = svg.node().clientWidth, H = svg.node().clientHeight;
        const cx = W / 2, cy = H / 2;
        const L = [...levels.keys()].sort((a, b) => a - b);
        const Rmin = 60, Rmax = Math.max(Math.min(W, H) * 0.45, 200);
        const ring = d3.scaleLinear().domain([0, Math.max(1, L.length - 1)]).range([Rmin, Rmax]);

        L.forEach((lvl, idx) => {
            const arr = levels.get(lvl);
            const m = arr.length || 1;
            arr.forEach((node, i) => {
                const a = (i / m) * 2 * Math.PI;
                const r = ring(idx);
                const x = cx + r * Math.cos(a), y = cy + r * Math.sin(a);
                node.x = x; node.y = y; node.fx = x; node.fy = y; node.pinMode = "layout";
            });
        });

        positionPinIcons();
        ticked();
        if (!frozen) simulation.alpha(0.001).restart();
    }

    // ---------- Fit to view ----------
    function fitToView(pad = 40) {
        const arr = nodes;
        const minX = d3.min(arr, d => d.x), maxX = d3.max(arr, d => d.x);
        const minY = d3.min(arr, d => d.y), maxY = d3.max(arr, d => d.y);
        const w = (maxX - minX) + 2 * pad, h = (maxY - minY) + 2 * pad;
        const midX = (minX + maxX) / 2, midY = (minY + maxY) / 2;
        const svgW = svg.node().clientWidth, svgH = svg.node().clientHeight;
        const k = Math.max(0.2, Math.min(4, 0.9 * Math.min(svgW / w, svgH / h)));
        const tx = svgW / 2 - k * midX, ty = svgH / 2 - k * midY;
        svg.transition().duration(500).call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(k));
    }

    // ---------- Persistence ----------
    function saveState() {
        const t = d3.zoomTransform(svg.node());
        const nodeState = {};
        nodes.forEach(n => { nodeState[n.id] = { x: n.x, y: n.y, fx: n.fx, fy: n.fy, pinMode: n.pinMode || null }; });
        const state = { layoutMode, radialFocusId, sizeByCount, frozen, transform: { k: t.k, x: t.x, y: t.y }, nodes: nodeState };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    }
    function loadState() {
        const s = localStorage.getItem(STORAGE_KEY);
        if (!s) return;
        try {
            const st = JSON.parse(s);
            layoutMode = st.layoutMode || layoutMode;
            radialFocusId = st.radialFocusId || radialFocusId;
            if (nodesMap.has(radialFocusId)) focusNodeSel.value = radialFocusId;
            sizeByCount = !!st.sizeByCount;
            frozen = !!st.frozen;

            if (st.nodes) {
                nodes.forEach(n => {
                    const ns = st.nodes[n.id]; if (!ns) return;
                    if (typeof ns.x === "number") n.x = ns.x;
                    if (typeof ns.y === "number") n.y = ns.y;
                    n.fx = (ns.fx == null ? null : ns.fx);
                    n.fy = (ns.fy == null ? null : ns.fy);
                    n.pinMode = ns.pinMode || null;
                });
                ticked();
            }
            if (st.transform) {
                const { k, x, y } = st.transform;
                svg.call(zoom.transform, d3.zoomIdentity.translate(x, y).scale(k));
            }
        } catch (e) { console.warn("State load failed:", e); }
    }

    // ---------- Controls ----------
    function showFocusIfNeeded() {
        focusRow.style.display = (layoutModeSel.value === "radial") ? "" : "none";
    }

    loadState();
    layoutModeSel.value = layoutMode;
    sizeByCountSel.checked = sizeByCount;
    freezeBtn.textContent = frozen ? "Unfreeze" : "Freeze";
    showFocusIfNeeded();
    applySizes();
    await applyLayout(layoutMode);

    layoutModeSel.addEventListener("change", async () => {
        await applyLayout(layoutModeSel.value);
        saveState();
    });

    focusNodeSel.addEventListener("change", () => {
        radialFocusId = focusNodeSel.value;
        if (layoutModeSel.value === "radial") runRadialLayout(radialFocusId);
        saveState();
    });

    sizeByCountSel.addEventListener("change", () => {
        sizeByCount = sizeByCountSel.checked;
        applySizes(); saveState();
    });

    fitBtn.addEventListener("click", () => fitToView());

    freezeBtn.addEventListener("click", () => {
        frozen = !frozen;
        if (frozen) simulation.stop(); else simulation.restart();
        freezeBtn.textContent = frozen ? "Unfreeze" : "Freeze";
        saveState();
    });

    unpinBtn.addEventListener("click", () => { unpinAll(); saveState(); });

    window.addEventListener("resize", () => {
        const w = svg.node().clientWidth, h = svg.node().clientHeight;
        simulation.force("center", d3.forceCenter(w / 2, h / 2));
        if (!frozen) simulation.alpha(0.1).restart();
    });

    window.addEventListener("beforeunload", saveState);

    function applySizes() {
        nodeCircle.attr("r", d => nodeRadius(d));
        positionPinIcons();
        simulation.force("collision", d3.forceCollide().radius(d => nodeRadius(d) + 4));
        if (!frozen) simulation.alpha(0.12).restart();
    }
})();