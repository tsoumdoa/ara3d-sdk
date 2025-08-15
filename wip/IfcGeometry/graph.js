// graph.js
// Define your graph data here. Keep this global for the page to read.
window.GRAPH_DATA = {
    nodes: {
        "IfcProject": { color: "#8c564b", count: 1 },
        "IfcSite": { color: "#17becf", count: 1 },
        "IfcBuilding": { color: "#1f77b4", count: 2 },
        "IfcBuildingStorey": { color: "#e15759", count: 5 },
        "IfcSpace": { color: "#2ca02c", count: 120 },
        "IfcDoor": { color: "#bcbd22", count: 42 },
        "IfcFurniture": { color: "#9467bd", count: 34 }
    },
    relations: {
        "IfcProject": [{ name: "IfcSite", count: 1 }],
        "IfcSite": [{ name: "IfcBuilding", count: 1 }],
        "IfcBuilding": [{ name: "IfcBuildingStorey", count: 5 }],
        "IfcBuildingStorey": [{ name: "IfcSpace", count: 120 }, { name: "IfcDoor", count: 42 }],
        "IfcSpace": { name: "IfcFurniture", count: 15 },
        "IfcFurniture": { name: "IfcSpace", count: 15 } // bi-directional example
    }
};