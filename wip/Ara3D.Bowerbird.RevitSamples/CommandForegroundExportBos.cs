using Autodesk.Revit.UI;
using System.Text;

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
        var sb = new StringBuilder();
        doc?.ExportBimOpenSchema(GetExportSettings(), sb);
        TextDisplayForm.DisplayText(sb.ToString());
    }
}