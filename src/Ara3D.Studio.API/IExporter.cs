using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IExporter
{
    public void Export(IReadOnlyList<Model3D> models, string filePath);
    public string FileType { get; }
    public string FileExtension { get; }
}