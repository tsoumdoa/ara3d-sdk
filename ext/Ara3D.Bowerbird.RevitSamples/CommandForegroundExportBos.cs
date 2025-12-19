using Autodesk.Revit.UI;
using System.Text;
using Ara3D.Logging;

namespace Ara3D.Bowerbird.RevitSamples;

public class CommandForegroundExportBos : NamedCommand
{
    public override string Name => "BOM Export";

    public BimOpenSchemaExportSettings GetExportSettings()
        => new()
        {
            Folder = BimOpenSchemaExportSettings.DefaultFolder,
            IncludeLinks = true,
            IncludeGeometry = true,
            UseCurrentView = false,
        };

    public override void Execute(object arg)
    {
        var uiapp = arg as UIApplication;
        var doc = uiapp?.ActiveUIDocument?.Document;
        var sb = new StringBuilder();
        var logger = Logger.Create(sb);
        doc?.ExportBimOpenSchema(GetExportSettings(), logger);
        TextDisplayForm.DisplayText(sb.ToString());
    }
}   