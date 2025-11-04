namespace Ara3D.Studio.Samples;

public class Merge : IModelModifier
{
    public Model3D Eval(Model3D m, EvalContext eval)
    {
        var mesh = m.ToMesh(); 
        var mat = m.FirstOrDefaultMaterial();
        return Model3D.Create(mesh, mat);
    }
}