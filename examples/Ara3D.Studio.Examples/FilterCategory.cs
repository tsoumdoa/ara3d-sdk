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
        throw new NotImplementedException();
        //return model3D.Filter(es => es.MeshIndex >= 0 && es.TransformIndex < col.Count && col[es.TransformIndex] is string s && KeepCategory(s));
    }
}