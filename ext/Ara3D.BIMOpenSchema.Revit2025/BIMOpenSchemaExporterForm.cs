using Ara3D.Bowerbird.RevitSamples;
using Ara3D.Utils;
using System;
using System.IO;
using System.Windows.Forms;
using Ara3D.Logging;

namespace Ara3D.BIMOpenSchema.Revit2025
{
    public partial class BIMOpenSchemaExporterForm : Form
    {
        public Autodesk.Revit.DB.Document CurrentDocument;
        public FilePath CurrentFilePath;

        public BIMOpenSchemaExporterForm()
        {
            InitializeComponent();
            buttonLuanchBOSExplorer.Enabled = OpenSchemaApp.BrowserAppPath.Exists();

            var defaultExportDir = DefaultFolder;
            try
            {
                defaultExportDir.Create();
            }
            catch (Exception)
            {
                defaultExportDir = "";
            }
            exportDirTextBox.Text = defaultExportDir;
            FormClosing += (_, args) =>
            {
                args.Cancel = true;
                Hide(); // Hide the form instead of closing it
            };
        }

        public void Show(Autodesk.Revit.DB.Document doc)
        {
            if (doc == null)
            {
                MessageBox.Show("No Revit document is available. Please open a Revit document first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CurrentDocument = doc;
            Show();
        }

        public static DirectoryPath DefaultFolder =>
            SpecialFolders.MyDocuments.RelativeFolder("BIM Open Schema");

        private void linkLabel1_Click(object sender, EventArgs e)
        {
            try
            {
                var url = @"https://github.com/ara3d/bim-open-schema";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chooseFolderButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.InitialDirectory = GetCurrentExportFolder().GetFullPath();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                exportDirTextBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        public BimOpenSchemaExportSettings GetExportSettings()
            => new()
            {
                Folder = GetCurrentExportFolder(),
                IncludeLinks = checkBoxIncludeLinks.Checked,
                IncludeGeometry = checkBoxMeshGeometry.Checked
            };

        public DirectoryPath CurrentDocumentDir
            => new FilePath(CurrentDocument.PathName).GetDirectory();

        public DirectoryPath GetCurrentExportFolder()
        {
            var folder = exportDirTextBox.Text;
            if (string.IsNullOrEmpty(folder))
            {
                folder = DefaultFolder;
            }
            return new DirectoryPath(folder);
        }

        public void Log(string s)
        {
            richTextBox1.BeginInvoke(() =>
            {
                try
                {
                    richTextBox1.AppendText(s + Environment.NewLine);
                }
                catch
                { }
            });
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            var settings = GetExportSettings();

            var folder = settings.Folder;
            try
            {
                if (!Directory.Exists(folder))
                {
                    MessageBox.Show($"The folder {folder} does not exist. Please choose a valid folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                folder.TestWrite();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The folder {folder} was not writeable. Error {ex.Message}. Please choose a different folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (CurrentDocument == null)
                {
                    MessageBox.Show("No active document found. Please open a Revit document and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var logWriter = LogWriter.Create(Log);
                var logger = new Logger(logWriter, "BOS Exporter");
                CurrentFilePath = CurrentDocument.ExportBimOpenSchema(settings, logger);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                MessageBox.Show($"An error occurred during export: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonLaunchBosExplorer_Click(object sender, EventArgs e)
        {
            if (!OpenSchemaApp.BrowserAppPath.Exists())
                MessageBox.Show("Could not find the browser application");

            if (CurrentFilePath.Exists())
                OpenSchemaApp.BrowserAppPath.Execute(CurrentFilePath.Value.Quote());
            else
                OpenSchemaApp.BrowserAppPath.Execute();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        { }

        private void buttonLaunchWindowsExplorer_Click(object sender, EventArgs e)
        {
            if (CurrentFilePath.Exists())
                CurrentFilePath.SelectFileInExplorer();
            else
                GetCurrentExportFolder().OpenFolderInExplorer();
        }

        private void buttonAra3D_Click(object sender, EventArgs e)
        {
            if (!OpenSchemaApp.Ara3dStudioExePath.Exists())
                MessageBox.Show("Could not find the path to Ara 3D Studio");

            if (CurrentFilePath.Exists())
                OpenSchemaApp.Ara3dStudioExePath.Execute(CurrentFilePath.Value.Quote());
            else
                OpenSchemaApp.Ara3dStudioExePath.Execute();
        }
    }
}
