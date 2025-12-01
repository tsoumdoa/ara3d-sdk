
using Ara3D.Utils;

namespace Ara3D.Bowerbird.RevitSamples;

public class BimOpenSchemaExportSettings
{
    public DirectoryPath Folder = DefaultFolder;
    public bool IncludeLinks = true;
    public bool IncludeGeometry = true;
    public string FileExtension = DefaultFileExtension;

    public static string DefaultFileExtension = "bos";

    public static DirectoryPath DefaultFolder 
        => SpecialFolders.MyDocuments.RelativeFolder("BIM Open Schema");
}