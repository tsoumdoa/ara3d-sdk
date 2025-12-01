using System.Reflection;

namespace Ara3D.Studio.Samples;

public static class LetterPolygons
{
    static List<Vector2> Rect(float x0, float y0, float x1, float y1)
        => new() { new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1) };

    static float T(float s, float max = 0.45f) 
        => Math.Max(1e-6f, Math.Min(s, max));

    public const float Stroke = 0.22f;

    public static IPolygon2D LetterI(float stroke = Stroke)
    {
        var t = T(stroke, 0.9f);
        var x0 = 0.5f - t * 0.5f; var x1 = 0.5f + t * 0.5f;
        var outer = Rect(x0, 0, x1, 1);
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterL(float stroke = Stroke)
    {
        var t = T(stroke, 0.9f);
        var s = t;
        var outer = new List<Vector2>
        {
            new(0,0), new(1,0), new(1,s), new(s,s), new(s,1), new(0,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterT(float stroke = Stroke)
    {
        var t = T(stroke, 0.9f);
        var cx0 = 0.5f - t * 0.5f; var cx1 = 0.5f + t * 0.5f;
        var y = 1 - t;
        var outer = new List<Vector2>
        {
            new(0,1), new(1,1), new(1,y), new(cx1,y), new(cx1,0),
            new(cx0,0), new(cx0,y), new(0,y)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterH(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var mid0 = 0.5f - t * 0.5f; var mid1 = 0.5f + t * 0.5f;
        var outer = new List<Vector2>
        {
            new(0,1), new(0,0), new(t,0), new(t,mid0), new(1-t,mid0),
            new(1-t,0), new(1,0), new(1,1), new(1-t,1), new(1-t,mid1),
            new(t,mid1), new(t,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterN(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var outer = new List<Vector2>
        {
            new(0,1), new(0,0), new(t,0), new(1-t,1), new(1,1), new(1,0),
            new(1-t,0), new(t,1), new(0,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterM(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var inset = Math.Max(0.05f, t * 0.7f);
        var outer = new List<Vector2>
        {
            new(0,0), new(t,0), new(0.5f,1), new(1-t,0), new(1,0), new(1,1),
            new(1-t,1), new(0.5f,inset), new(t,1), new(0,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterE(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var m = 0.5f - t * 0.5f;
        var outer = new List<Vector2>
        {
            new(0,1), new(1,1), new(1,1-t), new(t,1-t), new(t,m+t), new(0.7f, m+t),
            new(0.7f, m), new(t,m), new(t,t), new(1,t), new(1,0), new(0,0)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterF(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var m = 0.5f - t * 0.5f;
        var outer = new List<Vector2>
        {
            new(0,1), new(1,1), new(1,1-t), new(t,1-t), new(t,m+t), new(0.7f,m+t),
            new(0.7f,m), new(t,m), new(t,0), new(0,0)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterP(float stroke = Stroke)
    {
        var t = T(stroke, 0.4f);
        var outer = new List<Vector2>
        {
            new(0,0), new(t,0), new(t,0.6f), new(1,0.6f), new(1,1), new(0,1)
        };
        var hole = Rect(t + t * 0.3f, 0.6f + t * 0.2f, 1 - t * 0.2f, 1 - t * 0.2f);
        return CreatePolygon(outer, hole);
    }

    public static IPolygon2D LetterR(float stroke = Stroke)
    {
        var t = T(stroke, 0.4f);
        var outer = new List<Vector2>
        {
            new(0,0), new(t,0), new(t,0.6f), new(1,0.6f), new(1,1), new(0.6f,1),
            new(1,0), new(0.6f,0), new(t,0), new(0,0), // keep simple outline—bottom leg angled via corner
        };
        // Clean R: use simpler outer with angled leg and one hole for upper bowl
        outer = new()
        {
            new(0,0), new(t,0), new(t,0.55f), new(1,0.55f), new(1,1), new(0,1),
            new(0,0)
        };
        var hole = Rect(t + t * 0.3f, 0.55f + t * 0.2f, 1 - t * 0.2f, 1 - t * 0.2f);
        // Add a small triangular bite to form the diagonal leg by subtracting nothing (kept blocky R); if you need a cut, do boolean difference afterward.
        return CreatePolygon(outer, hole);
    }

    public static IPolygon2D LetterY(float stroke = Stroke)
    {
        var t = T(stroke, 0.4f);
        var s = t * 0.5f;
        var outer = new List<Vector2>
        {
            new(0,1), new(s,1), new(0.5f,0.5f+s), new(1-s,1), new(1,1),
            new(0.6f,0.6f), new(0.6f,0), new(0.4f,0), new(0.4f,0.6f), new(0,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterC(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var gap = Math.Max(0.12f, t * 0.6f);
        var outer = new List<Vector2>
        {
            new(0,0), new(1-gap,0), new(1-gap,t), new(t,t), new(t,1-t),
            new(1-gap,1-t), new(1-gap,1), new(0,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterU(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var outer = new List<Vector2>
        {
            new(0,1), new(0,0), new(1,0), new(1,1), new(1-t,1), new(1-t,t),
            new(t,t), new(t,1), new(0,1)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterV(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var a = Math.Max(0.05f, t * 0.5f);
        var outer = new List<Vector2>
        {
            new(0,1), new(a,1), new(0.5f, t*0.3f), new(1-a,1), new(1,1), new(0.5f,0)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterX(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var s = t * 0.5f;
        var outer = new List<Vector2>
        {
            new(0, s), new(s,0), new(0.5f,0.5f - s), new(1 - s,0), new(1, s),
            new(0.5f + s,0.5f), new(1,1 - s), new(1 - s,1), new(0.5f,0.5f + s),
            new(s,1), new(0,1 - s), new(0.5f - s,0.5f)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterZ(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var outer = new List<Vector2>
        {
            new(0,1), new(1,1), new(1,1-t), new(t,1-t), new(1-t,t), new(1,t),
            new(1,0), new(0,0), new(0,t), new(1-t,t), new(t,1-t), new(0,1-t)
        };
        return CreatePolygon(outer);
    }

    public static IPolygon2D LetterO(float stroke = Stroke)
    {
        var t = T(stroke, 0.45f);
        var outer = Rect(0, 0, 1, 1);
        var inner = Rect(t, t, 1 - t, 1 - t);
        return CreatePolygon(outer, inner);
    }

    public static IPolygon2D LetterA(float stroke = Stroke)
    {
        var t = T(stroke, 0.35f);
        var leg = Math.Max(0.06f, t * 0.6f);
        var outer = new List<Vector2>
        {
            new(0+leg,0), new(0+leg+t,0), new(0.5f,1), new(1-leg-t,0), new(1-leg,0), new(0.5f,1 - t*0.2f)
        };
        var holeTop = 0.62f;
        var holeW = Math.Max(0.15f, t * 1.5f);
        var inner = new List<Vector2>
        {
            new(0.5f, holeTop + 0.25f),
            new(0.5f - holeW, holeTop - 0.02f),
            new(0.5f + holeW, holeTop - 0.02f),
        };
        return CreatePolygon(outer, inner);
    }

    public static IPolygon2D LetterS(float stroke = Stroke)
    {
        var t = T(stroke, 0.35f);
        var o = new List<Vector2>
        {
            new(0,0+t), new(0,0), new(1,0), new(1,t), new(t,t), new(1-t,0.5f),
            new(1,0.5f), new(1,1), new(0,1), new(0,1-t), new(1-t,1-t), new(t,0.5f),
        };
        return CreatePolygon(o);
    }

    public static IPolygon2D LetterCappedO(float stroke = Stroke) // variant ring (example extra)
    {
        var t = T(stroke, 0.45f);
        var outer = Rect(0, 0, 1, 1);
        var inner = Rect(t, t, 1 - t, 1 - t);
        return CreatePolygon(outer, inner);
    }

    public static SimplePolygonWithHoles CreatePolygon(IReadOnlyList<Vector2> boundary,
        params IReadOnlyList<Vector2>[] holes)
        => boundary.ToPolygonWithHoles(holes);
}

public class LetterDemo : IModelGenerator
{
    public static List<MethodInfo> LetterFuncs =>
        typeof(LetterPolygons).GetMethods().Where(m => m.Name.StartsWith("Letter")).ToList();

    public List<string> LetterNames 
        => LetterFuncs.Select(mi => mi.Name.Substring("Letter".Length)).ToList();

    [Options(nameof(LetterNames))] public int Letter { get; set; }

    [Range(0f, 1f)] public float Stroke { get; set; } = 0.22f;
    public bool UseEarCut { get; set; } = true;

    public IModel3D Eval(EvalContext context)
    {
        var func = LetterFuncs[Letter]; 
        var poly = func.Invoke(null, [Stroke]) as IPolygon2D;
        if (poly == null) return null;
        var triangles = UseEarCut 
            ? poly.TriangulateEarClipping() 
            : poly.TrianglesFan();
        var mesh = triangles.ToMesh();
        return Model3D.Create(mesh);
    }
}