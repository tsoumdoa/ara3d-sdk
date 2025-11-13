using Ara3D.BimOpenSchema;
using Ara3D.BimOpenSchema.IO;
using Ara3D.Logging;
using Ara3D.Utils;

namespace Ara3D.BIMOpenSchema.Tests;

public static class NewBimTests
{
    public static FilePath TestFile = @"C:\Users\cdigg\data\bos\snowdon.parquet.zip";

    [Test]
    public static void TestLoadBimDataAndBimGeometry()
    {
        var logger = Logger.Console;
        logger.Log("Loading BIM Geometry");
        var bg = TestFile.ReadBimGeometryFromParquetZip();
        logger.Log("Loading BIM Data");
        var bd = TestFile.ReadBimDataFromParquetZip();
        logger.Log("Loaded data");
        OutputBimGeometry(logger, bg);
        OutputBimData(logger, bd);
    }

    public static void OutputBimData(ILogger logger, BimData bd)
    {
        logger.Log($"# documents = {bd.Documents.Count}");
        logger.Log($"# entities = {bd.Entities.Count}");
        logger.Log($"# descriptors = {bd.Descriptors.Count}");
        logger.Log($"# points = {bd.Points.Count}");
        logger.Log($"# string = {bd.Strings.Count}");
        logger.Log($"# string parameters = {bd.StringParameters.Count}");
        logger.Log($"# point parameters  = {bd.PointParameters.Count}");
        logger.Log($"# integer parameters = {bd.IntegerParameters.Count}");
        logger.Log($"# double parameters = {bd.DoubleParameters.Count}");
        logger.Log($"# entity parameters = {bd.EntityParameters.Count}");
        logger.Log($"# relations = {bd.Relations.Count}");
    }

    public static void OutputBimGeometry(ILogger logger, BimGeometry bimGeometry)
    {
        logger.Log($"# transforms = {bimGeometry.GetNumTransforms()}");
        logger.Log($"# meshes = {bimGeometry.GetNumMeshes()}");
        logger.Log($"# elements = {bimGeometry.GetNumElements()}");
        logger.Log($"# faces = {bimGeometry.GetNumFaces()}");
        logger.Log($"# vertices = {bimGeometry.GetNumVertices()}");
        logger.Log($"# materials = {bimGeometry.GetNumMaterials()}");
    }
}