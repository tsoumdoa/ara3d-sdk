namespace Ara3D.Studio.Samples
{
    public class Torus : IModelGenerator
    {
        public Vector2 ToUv(int i, int j)
            => (i / (float)NumColumns, j / (float)NumRows);

        public Point3D PointOnTorus(int i, int j)
            => ToUv(i, j).Torus(MajorRadius, MinorRadius);

        private QuadGrid3D GetQuadGrid()
        {
            var points = new FunctionalReadOnlyList2D<Point3D>(NumColumns, NumRows, PointOnTorus);
            return new QuadGrid3D(points, ClosedX, ClosedY);
        }

        [Range(0f, 10f)] public float MajorRadius { get; set; } = 2f;
        [Range(0f, 10f)] public float MinorRadius { get; set; } = 0.2f;

        [Range(2, 64)] public int NumRows { get; set; } = 16;
        [Range(2, 64)] public int NumColumns { get; set; } = 16;
        
        public bool ClosedX;
        public bool ClosedY;
        
        public IModel3D Eval(EvalContext context)
            => Model3D.Create(GetQuadGrid().Triangulate());
    }
}