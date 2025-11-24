public class GlbDataSet: IModelModifier
{
    public string TableNames { get; private set; }

    public string ObjectColumns { get; private set; }
    public int ObjectRowCount { get; private set; }

    public string MeshColumns { get; private set; }
    public int MeshRowCount { get; private set; }

    public string MaterialColumns { get; private set; }
    public int MaterialRowCount { get; private set; }

    public Model3D Eval(Model3D model3D, EvalContext context)
    {
        var ds = model3D.DataSet;

        TableNames = ds?.Tables.Select(t => $"{t.Name}").JoinStrings() ?? "";
        
        var objects = ds?.GetTable("Object");
        ObjectRowCount = objects?.Rows.Count ?? 0;
        ObjectColumns = objects == null ? "" 
            : objects.Columns.Select(c => c.Descriptor.Name).JoinStrings();

        var meshes = ds?.GetTable("Mesh");
        MeshRowCount = meshes?.Rows.Count ?? 0;
        MeshColumns = meshes == null ? "" 
            : meshes.Columns.Select(c => c.Descriptor.Name).JoinStrings();
        
        var materials = ds?.GetTable("Material");
        MaterialRowCount = materials?.Rows.Count ?? 0;
        MaterialColumns = materials == null ? "" 
            : materials.Columns.Select(c => c.Descriptor.Name).JoinStrings();
        
        return model3D;

    }
}