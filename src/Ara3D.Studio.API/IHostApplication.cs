using Ara3D.Logging;
using Ara3D.Models;

namespace Ara3D.Studio.API;

public interface IHostApplication
{
    IEnumerable<IModel3D> GetModels();
    ILogger Logger { get; }
    void Invalidate(object obj);
}
