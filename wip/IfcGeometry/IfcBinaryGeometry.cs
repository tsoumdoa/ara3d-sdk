namespace Ara3D.IfcGeometry;


public class IfcBinaryGeometry
{
    /// <summary>
    /// The X coordinate of each point
    /// </summary>
    public List<float> PointXs = [];

    /// <summary>
    /// The Y coordinate of reach point
    /// </summary>
    public List<float> PointYs = [];

    /// <summary>
    /// The Z coordinate of reach point
    /// </summary>
    public List<float> PointZs = [];
    
    /// <summary>
    /// The indices of the loop points in order 
    /// </summary>
    public List<int> LoopPoints = [];
    
    /// <summary>
    /// The index into LoopPoints (one of these per loop)
    /// </summary>
    public List<int> LoopPointOffset = [];

    /// <summary>
    /// The original ID of each face
    /// </summary>
    public List<int> FaceIds = [];

    /// <summary>
    /// Each faces index in the Face loops 
    /// </summary>
    public List<int> FaceLoopOffsets = [];

    /// <summary>
    /// Loops for each face
    /// </summary>
    public List<int> FaceLoops = [];

    /// <summary>
    /// The orientation of each face loop 
    /// </summary>
    public List<bool> FaceLoopOrientations = [];

    /// <summary>
    /// The original ID of each point
    /// </summary>
    public List<int> PointIds = [];

    /// <summary>
    /// The original ID of each loop
    /// </summary>
    public List<int> LoopIds = [];

    /// <summary>
    /// The original ID of each bounds
    /// </summary>
    public List<int> BoundsIds = [];
}