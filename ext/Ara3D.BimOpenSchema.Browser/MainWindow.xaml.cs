using Ara3D.BimOpenSchema.IO;
using Ara3D.DataTable;
using Ara3D.IO.GltfExporter;
using Ara3D.Models;
using Ara3D.Utils;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace Ara3D.BimOpenSchema.Browser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BimData Data;
        public BimModel3D Model3D;
        public BimObjectModel ObjectModel => Model3D.ObjectModel;
        public IReadOnlyList<IDataTable> Tables;
        public Grouping CurrentGrouping = Grouping.None;
        public IReadOnlyList<IGrouping<string, EntityModel>> GroupedEntities = null;
        public FilePath CurrentFile;
        public OpenFileDialog OpenFileDialog = null;
        public FolderBrowserDialog FolderDialog = null;

        public enum Grouping
        {
            None,
            Document,
            //CategoryType,
            Level,
            Group,
            Room,
            Class,
            Category,
            FamilyType, 
        }

        public MainWindow()
        {
            InitializeComponent();
            UpdateGroupingMenuItems();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    await OpenFile(args[1]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured {ex.Message}");
            }
        }

        public static DirectoryPath DefaultSaveLocation()
            => SpecialFolders.MyDocuments.RelativeFolder("BIM Open Schema");

        public static void SaveToGltf(BimModel3D model, FilePath filePath)
        {
            var builder = new GltfBuilder();
            builder.SetModel(model.Model3D);
            var bytes = new List<byte>();
            var data = builder.Build(bytes);
            data.Export(bytes, filePath);
        }

        public async Task OpenFile(FilePath fp)
        {
            if (!fp.Exists())
                return;
            using var waitContext = new WpfWaitContext();

            Model3D = null;
            CurrentFile = fp;
            Data = await fp.ReadBimDataFromParquetZipAsync().ConfigureAwait(false);
            Model3D = BimModel3D.Create(Data);
            await UpdateTables();
        }

        public void UpdateGroupingMenuItems()
        {
            GroupingMenuItem.Items.Clear();
            foreach (var val in Enum.GetValues(typeof(Grouping)))
            {
                var tmp = new MenuItem()
                {
                    Header = Enum.GetName(typeof(Grouping), val),
                    IsCheckable = true,
                };
                if (CurrentGrouping == (Grouping)val)
                {
                    tmp.IsChecked = true;
                }

                tmp.Click += (_, _) => SetGrouping((Grouping)val);
                GroupingMenuItem.Items.Add(tmp);
            }
        }

        public async Task SetGrouping(Grouping g)
        {
            if (g == CurrentGrouping)
                return;
            CurrentGrouping = g;
            UpdateGroupingMenuItems();
            await UpdateTables();
        }

        public DirectoryPath ChooseFolder()
        {
            if (FolderDialog == null)
            {
                FolderDialog = new FolderBrowserDialog();
                var startFolder = DefaultSaveLocation();
                startFolder.Create();
                FolderDialog.InitialDirectory = startFolder;
            }

            if (FolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return null;

            var baseName = CurrentFile.GetFileNameWithoutExtension();
            var baseFolder = new DirectoryPath(FolderDialog.SelectedPath);
            var folder = baseFolder.RelativeFolder(baseName);
            if (CurrentGrouping != Grouping.None)
            {
                var subFolder = CurrentGrouping.ToString();
                folder = folder.RelativeFolder(subFolder);
            }
            folder.Create();
            return folder;
        }

        public IEnumerable<IGrouping<string, EntityModel>> CreateGroupings()
        {
            switch (CurrentGrouping)
            {
                case Grouping.None:
                    return ObjectModel.Entities.GroupBy(_ => "All");
                case Grouping.Category:
                    return ObjectModel.Entities.GroupBy(e => e.Category);
                case Grouping.Level:
                    return ObjectModel.Entities.GroupBy(e => e.LevelName);
                case Grouping.Group:
                    return ObjectModel.Entities.GroupBy(e => e.GroupName);
                case Grouping.Class:
                    return ObjectModel.Entities.GroupBy(e => e.ClassName);
                case Grouping.Room:
                    return ObjectModel.Entities.GroupBy(e => e.RoomName);
                case Grouping.Document:
                    return ObjectModel.Entities.GroupBy(e => e.DocumentTitle);
                case Grouping.FamilyType:
                    return ObjectModel.Entities.GroupBy(e => e.FamilyType);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //case Grouping.CategoryType:
        //    return ObjectModel.Entities.GroupBy(e => e.CategoryType);

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ??= new OpenFileDialog()
            {
                DefaultExt = ".bos",
                Filter = "BIM Open Schema files (*.bos)|*.bos|All files (*.*)|*.*"
            };

            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await OpenFile(OpenFileDialog.FileName);
            }
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (Tables == null)
            {
                MessageBox.Show("No data loaded", "Error");
                return;
            }

            var folder = ChooseFolder();
            if (!folder?.Exists() == true)
                return;

            using var waitContext = new WpfWaitContext();

            foreach (var t in Tables)
            {
                var fp = folder.RelativeFile(t.Name.ToValidFileName() + ".xlsx");
                t.WriteToExcel(fp);
            }
        }
        
        private async void ExportGLB_Click(object sender, RoutedEventArgs e)
        {
            if (Tables == null)
            {
                MessageBox.Show("No data loaded", "Error");
                return;
            }

            var folder = ChooseFolder();
            if (!folder?.Exists() == true)
                return;

            using var waitContext = new WpfWaitContext();

            foreach (var g in GroupedEntities)
            {
                var fp = folder.RelativeFile(g.Key.ToValidFileName() + ".glb");
                SaveGltf(g, fp);
            }
        }
        
        public void SaveGltf(IEnumerable<EntityModel> entities, FilePath fp)
        {
            var entityIndices = entities.Select(em => (int)em.Index).ToHashSet();
            var newModel = Model3D.Model3D.FilterAndRemoveUnusedMeshes(i => entityIndices.Contains(i.EntityIndex));
            if (newModel.Instances.Count > 0 && newModel.Meshes.Count > 0)
                newModel.WriteGlb(fp);
        }

        private async void ExportParquet_Click(object sender, RoutedEventArgs e)
        {
            var folder = ChooseFolder();
            if (!folder?.Exists() == true)
                return;

            if (!File.Exists(CurrentFile))
                return;

            try
            {
                using var waitContext = new WpfWaitContext();

                var fs = new FileStream(CurrentFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

                foreach (var entry in zip.Entries
                             .Where(e => e.Name.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase))
                             .OrderBy(e => e.FullName))
                {
                    var newFile = folder.RelativeFile(entry.Name);
                    await using var outFileStream = newFile.OpenWrite();
                    await using var entryStream = entry.Open();
                    await entryStream.CopyToAsync(outFileStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured when exporting parquet files: {ex}");
            }
        }

        private async void ExportSplitBOS_Click(object sender, RoutedEventArgs e)
        {
            if (GroupedEntities.Count <= 1)
            {
                MessageBox.Show("Exporting split BOS files requires one of the grouping options to be used");
                return;
            }

            var folder = ChooseFolder();
            if (!folder?.Exists() == true)
                return;

            if (!File.Exists(CurrentFile))
                return;

            try
            {
                // TODO: create new BOS files. 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured when exporting BOS files: {ex}");
            }
        }

        public DataTableFromEntities CreateTable(IGrouping<string, EntityModel> entities)
            => new (entities.ToList(), entities.Key, IncludeParamsMenuItem.IsChecked && CurrentGrouping != Grouping.None);

        private async Task UpdateTables()
        {
            using var waitContext = new WpfWaitContext();

            GroupedEntities = CreateGroupings().OrderBy(g => g.Key).ToList();

            await Dispatcher.InvokeAsync(() =>
            {
                Tables = GroupedEntities.Select(CreateTable).ToList();

                TabControl.Items.Clear();
                foreach (var t in Tables)
                {
                    var grid = TabControl.AddDataGridTab(t.Name);
                    grid.AssignDataTable(t);
                }
            });
        }

        private async void IncludeParamsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            await UpdateTables();
        }
    }
}   