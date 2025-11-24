namespace Ara3D.Studio.Samples;

public class FilterCategory : IModelModifier
{
    public bool KeepCategory(string s)
    {
        s = s.ToLowerInvariant();
        return s.Contains("elec") || s.Contains("cond") || s.Contains("light") || s.Contains("center") || s.Contains("device") || s.Contains("data");// || s.Contains("specialty");
    }

    public Model3D Eval(Model3D model3D, EvalContext context)
    {
        var dataSet = model3D.DataSet;
        var table = dataSet.Tables[0];
        var col = table.GetColumn("Category");
        return model3D.Where((node, i) => node.MeshIndex >= 0 && col[i] is string s && KeepCategory(s));
    }
}