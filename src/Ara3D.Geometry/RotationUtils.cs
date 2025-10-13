namespace Ara3D.Geometry;

public static class RotationUtils
{
    /// <summary>
    /// Returns a quaternion that rotates vector <paramref name="from"/> to align with <paramref name="to"/>.
    /// </summary>
    /*
    public static Quaternion RotateTo(this Vector3 from, Vector3 to, float epsilon = 1e-6f)
    {
        var aLen = from.Length();
        var bLen = to.Length();
        if (aLen < epsilon || bLen < epsilon)
            return Quaternion.Identity; // undefined rotation; choose identity

        var a = from / aLen;
        var b = to / bLen;

        var dot = a.Dot(b);

        // Almost identical direction
        if (dot > 1f - 1e-6f)
            return Quaternion.Identity;

        // Almost opposite direction: 180° around any axis orthogonal to 'a'
        if (dot < -1f + 1e-6f)
        {
            // Pick an orthogonal axis: try X, else Y
            Vector3 axis = Vector3.Cross(a, Vector3.UnitX);
            if (axis.LengthSquared() < 1e-12f)
                axis = Vector3.Cross(a, Vector3.UnitY);
            axis = axis.Normalize;
            return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
        }

        // General case
        var v = Vector3.Cross(a, b);
        // Closed-form quaternion (normalized):
        // q = [v * (1/s), s/2], where s = sqrt((1+dot)*2)
        float s = MathF.Sqrt((1f + dot) * 2f);
        float invS = 1f / s;

        var q = new Quaternion(v.X * invS, v.Y * invS, v.Z * invS, s * 0.5f);
        
        // Ensure unit quaternion (good practice against FP drift)
        return q.Normalize;
    }
    */
    public static Quaternion RotateTo(this Vector3 from, Vector3 to, float epsilon = 1e-6f)
    {
        float aLen = from.Length();
        float bLen = to.Length();
        if (aLen < epsilon || bLen < epsilon)
            return Quaternion.Identity;

        var a = from / aLen;
        var b = to / bLen;

        // Clamp dot for safety
        float dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);

        // Same direction
        if (dot > 1f - 1e-6f)
            return Quaternion.Identity;

        // Opposite direction: 180° around any axis orthogonal to 'a'
        if (dot < -1f + 1e-6f)
        {
            // Pick an orthogonal axis robustly
            Vector3 axis = Vector3.Cross(a, MathF.Abs(a.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY);
            axis = axis.Normalize;
            return Quaternion.CreateFromAxisAngle(axis, MathF.PI); // already unit
        }

        // Stable general case: q ~ [a×b, 1 + a·b], then normalize
        Vector3 c = Vector3.Cross(a, b);
        var q = new Quaternion(c.X, c.Y, c.Z, 1f + dot);
        q = q.Normalize;

        return q;
    }
    
    public static Matrix4x4 ToBoxTransform(this Line3D line, float thickness, float height)
        => Matrix4x4.CreateScale(line.Length, thickness, height) 
               * Vector3.UnitX.RotateTo(line.Direction) 
               * Matrix4x4.CreateTranslation(line.Center);
}