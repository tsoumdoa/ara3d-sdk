using Ara3D.Logging;
using Ara3D.Models;
using Ara3D.Utils;

namespace Ara3D.Studio.API;

public class ModelAsset : IModelAsset
{
    public string Name { get; set; }
    public FilePath FilePath { get; }
    public IModelLoader Loader { get; }
    public Model3D? Model { get; private set; }
    public string FileType => FilePath.GetExtension();

    public ModelAsset(FilePath filePath, IModelLoader loader)
    {
        FilePath = filePath;
        Name = FilePath.GetFileName();
        Loader = loader;
    }

    public async Task<Model3D> Import(ILogger logger)
    {
        logger.Log($"STARTED loading model from {FilePath} using the {Loader.GetType().Name} loader");
        Model = await Loader.Import(FilePath, logger);
        logger.Log($"COMPLETED loading model from {FilePath}");
        CheckModel(Model, logger);
        return Model;
    }

    public static void CheckModel(Model3D model, ILogger logger)
    {
        logger.Log("Checking model");
        logger.Log($"# instances = {model.Instances.Count}");
        logger.Log($"# meshes = {model.Meshes.Count}");
        var nMeshOutOfRange = model.Instances.Count(i => i.MeshIndex < 0 || i.MeshIndex >= model.Meshes.Count);
        logger.Log($"# instances with meshes out of range = {nMeshOutOfRange}");
        var nTransparent = model.Instances.Count(i => i.Transparent);
        logger.Log($"# instances where transparent = {nTransparent}");
    }

    public Model3D Eval(EvalContext context)
    {
        if (Model != null)
            return Model;
        var task = Import(context.Application.Logger);
        task.RunSynchronously();
        return task.Result;
    }
}