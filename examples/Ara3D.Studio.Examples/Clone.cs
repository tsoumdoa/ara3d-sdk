namespace Ara3D.Studio.Samples;

public class Clone : IModelModifier
{
    [Range(1, 100)] public int Rows = 2;
    [Range(1, 100)] public int Columns = 2;
    [Range(1f, 20f)] public float Spacing = 7f;
    
    public IModel3D Eval(IModel3D model, EvalContext eval)
    {
        var offset = MathF.Pow(2, Spacing);
        var positions = new List<Vector3>();
        
        for (var i = 0; i < Columns; i++)
        {
            for (var j = 0; j < Rows; j++)
            {
                positions.Add(new Vector3(i * offset, j * offset, 0));
            }
        }

        return model.Clone(positions);
    }
}