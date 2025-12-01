namespace Ara3D.Studio.Samples;

public class PushNodes : IModelModifier
{
    [Range(-10f, 10f)]
    public float Amount = 2;

    public static Matrix4x4 Push(Matrix4x4 self, Vector3 center, float amount)
    {
        var vec = (Vector3)self.Value.Translation - center;
        var newPos = center + vec * amount;
        return self.WithTranslation(newPos);   
    }

    public IModel3D Eval(IModel3D m, EvalContext eval)
    {
        if (m.Instances.Count == 0) return m;
        var center = m.GetBounds().Center;
        return m.WithInstances(node => node.WithMatrix(Push(node.Matrix4x4, center, Amount)));
    }   
}