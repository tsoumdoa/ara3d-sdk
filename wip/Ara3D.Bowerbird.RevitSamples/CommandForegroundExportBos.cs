using Autodesk.Revit.UI;

namespace Ara3D.Bowerbird.RevitSamples;

public class CommandForegroundExportBos : NamedCommand
{
    public override string Name => "BOM Export";

    public BimOpenSchemaExportSettings GetExportSettings()
        => new()
        {
            Folder = @"C:\Users\cdigg\data\bos",
            IncludeLinks = true,
            IncludeGeometry = true
        };

    public override void Execute(object arg)
    {
        var uiapp = arg as UIApplication;
        var doc = uiapp?.ActiveUIDocument?.Document;
        doc?.ExportBimOpenSchema(GetExportSettings());
    }
}