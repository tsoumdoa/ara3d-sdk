namespace Ara3D.Models;

public interface IModel3D : IDisposable
{
    void UpdateScene(RenderScene scene);
}