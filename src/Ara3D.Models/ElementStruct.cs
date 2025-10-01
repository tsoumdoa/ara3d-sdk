namespace Ara3D.Models;

/// <summary>
/// An element is a part of a 3D model. It has a mesh, a material, and a transformation.
/// The element struct uses indices to reference the data stored in the parent model.
/// </summary>
public readonly struct ElementStruct
{
    public ElementStruct(int entityIndex, int materialIndex, int meshIndex, int transformIndex)
    {
        EntityIndex = entityIndex;
        MaterialIndex = materialIndex;
        MeshIndex = meshIndex;
        TransformIndex = transformIndex;
    }

    public int EntityIndex { get; }
    public int MaterialIndex { get; }
    public int MeshIndex { get; }
    public int TransformIndex { get; }
}