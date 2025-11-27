using Ara3D.Bowerbird.RevitSamples;
using Ara3D.Logging;
using Ara3D.Utils;
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Ara3D.BIMOpenSchema.Revit2025
{
    public class OpenSchemaApp : IExternalApplication
    {
        public static OpenSchemaApp Instance { get; private set; }
        public RevitContext RevitContext { get; private set; }
        public UIControlledApplication UicApp { get; private set; }
        public UIApplication UiApp { get; private set; }
        public CommandExecutor CommandExecutor { get; set; }
        public BIMOpenSchemaExporterForm Form { get; private set; }

        public Result OnShutdown(UIControlledApplication application)
            => Result.Succeeded;

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            var bmpImg = new BitmapImage();
            bmpImg.BeginInit();
            bmpImg.StreamSource = memory;
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.EndInit();
            return bmpImg;
        }

       public Bitmap GetImage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Ara3D.BIMOpenSchema.Revit2025.bos32x32.png");
            return new Bitmap(stream);
        }

        public Result OnStartup(UIControlledApplication application)
        {
            UicApp = application;
            Instance = this;
            
            var logger = new Logger(LogWriter.DebugWriter, "BIMOpenSchema");
            CommandExecutor = new CommandExecutor(logger);
            RevitContext = new RevitContext(logger);
            
            // Store a reference to the UIApplication
            application.Idling += (sender, args) =>
            {
                if (UiApp == null)
                {
                    UiApp = sender as UIApplication;
                }
            };

            var rvtRibbonPanel = application.CreateRibbonPanel("BIM Open Schema");
            var pushButtonData = new PushButtonData("Parquet Exporter", "Export to Parquet", 
                Assembly.GetExecutingAssembly().Location,
                typeof(OpenSchemaExternalCommand).FullName);
            // https://www.revitapidocs.com/2020/544c0af7-6124-4f64-a25d-46e81ac5300f.htm
            if (!(rvtRibbonPanel.AddItem(pushButtonData) is PushButton runButton))
                return Result.Failed;
            runButton.LargeImage = BitmapToImageSource(GetImage());
            runButton.ToolTip = "Export BIM data as Parquet files conforming to the BIM Open Schema.";

            return Result.Succeeded;
        } 

        public void Run(UIApplication application)
        {
            if (UiApp == null)
            {
                UiApp = application;
            }

            if (Form == null)
            {
                Form = new BIMOpenSchemaExporterForm();
            }

            Form.Show(UiApp.ActiveUIDocument?.Document, Export);
        }

        public void ReportProgress(int current, int count)
        {
            try
            {
                if (current % 10 == 0)
                {
                    if (Form.InvokeRequired)
                        Form.BeginInvoke(() => Form.UpdateProgress(current, count));
                    else
                        Form.UpdateProgress(current, count);
                }
            }
            catch
            { }
        }

        public void Export(BimOpenSchemaExportSettings settings)
        {
            var currentDoc = UiApp.ActiveUIDocument?.Document;
            if (currentDoc == null)
            {
                MessageBox.Show("No active document found. Please open a Revit document and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                var timer = Stopwatch.StartNew();
                currentDoc.ExportBimOpenSchema(settings, sb);
                timer.Stop();

                if (MessageBox.Show($"Completed export process in {timer.Elapsed.TotalSeconds:F1} seconds.\nOpen output folder?", "Congratulations!",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    settings.Folder.OpenFolderInExplorer();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred during export: {ex.Message}.", "Error");
            }

            ReportProgress(0, 1); 
        }
    }
}
