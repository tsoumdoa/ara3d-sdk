namespace Ara3D.Studio.Samples;

public class AiCategoryFilter : IModelModifier
{
    public string Category { get; set; }

    public void CategoryChanged(object obj, string cat)
    {
        Category = cat;
        _app.Invalidate(this);
    }

    private IHostApplication _app;

    public IModel3D Eval(IModel3D model, EvalContext eval)
    {
        if (_app == null)
        {
            _app = eval.Application;
            _app.OnCategoryChanged += CategoryChanged;
        }

        /*
        var dataSet = model.DataSet;
        var table = dataSet.Tables[0];
        var col = table.GetColumn("Category");
        return model.Where((node, i) => node.MeshIndex >= 0 
            && col[i]?.Equals(Category) == true);
        */

        // TODO: 
        throw new NotImplementedException();
        return model;
    }
}