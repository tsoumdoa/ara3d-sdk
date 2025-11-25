using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Ara3D.BimOpenSchema.IO;
using Ara3D.DataTable;
using Ara3D.IO.GltfExporter;
using Ara3D.Models;
using Ara3D.Utils;
using Microsoft.Win32;

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
        public FilePath CurrentFile;
        public OpenFileDialog OpenFileDialog = null;
        public SaveFileDialog SaveParquetFileDialog = null;
        public SaveFileDialog SaveExcelFileDialog = null;

        public enum Grouping
        {
            None,
            Document,
            CategoryType,
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
            await OpenFile(@"C:\Users\cdigg\data\bos\Snowdon Towers Sample Architectural.parquet.zip");
        }

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
            Model3D = null;
            CurrentFile = fp;
            Data = await fp.ReadBimDataFromParquetZipAsync().ConfigureAwait(false);
            Model3D = BimModel3D.Create(Data);
            SaveToGltf(Model3D, @"C:\Users\cdigg\data\bos\output\test.glb");    
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

        public async void SetGrouping(Grouping g)
        {
            if (g == CurrentGrouping)
                return;
            CurrentGrouping = g;
            UpdateGroupingMenuItems();
            await UpdateTables();
        }

        public IEnumerable<IGrouping<string, EntityModel>> CreateGroupings()
        {
            switch (CurrentGrouping)
            {
                case Grouping.None:
                    return ObjectModel.Entities.GroupBy(_ => "All");
                case Grouping.CategoryType:
                    return ObjectModel.Entities.GroupBy(e => e.CategoryType);
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
        
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ??= new OpenFileDialog()
            {
                DefaultExt = ".zip",
                Filter = "Zipped parquet files (*.zip)|*.zip|All files (*.*)|*.*"
            };

            if (OpenFileDialog.ShowDialog() == true)
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

            var folder = CurrentFile.RelativeFolder(CurrentFile.GetFileNameWithoutExtension());

            foreach (var t in Tables)
            {
                var fp = folder.RelativeFile(t.Name.ToValidFileName() + ".xlsx");
                t.WriteToExcel(fp);
            }
        }

        private async void ExportGLB_Click(object sender, RoutedEventArgs e)
        {
            var folder = CurrentFile.RelativeFolder(CurrentFile.GetFileNameWithoutExtension());

            foreach (var t in Tables)
            {
                var fp = folder.RelativeFile(t.Name.ToValidFileName() + ".xlsx");
                t.WriteToExcel(fp);
            }
        }

        private async void ExportParquet_Click(object sender, RoutedEventArgs e)
        {
            if (Tables == null)
            {
                MessageBox.Show("No data loaded", "Error");
                return;
            }

            SaveParquetFileDialog ??= new SaveFileDialog()
            {
                DefaultExt = ".zip",
                Filter = "Zipped Parquet files (*.zip)|*.zip|All files (*.*)|*.*"
            };

            if (SaveParquetFileDialog.ShowDialog() == true)
            {
                var fp = new FilePath(SaveParquetFileDialog.FileName);
                var ds = Tables.ToDataSet();
                await ds.WriteParquetToZipAsync(fp);
            }
        }
        
        public DataTableFromEntities CreateTable(IGrouping<string, EntityModel> entities)
            => new (entities.ToList(), entities.Key, IncludeParamsMenuItem.IsChecked && CurrentGrouping == Grouping.None);

        private async Task UpdateTables()
        {
            var groupings = CreateGroupings().OrderBy(g => g.Key).ToList();

            await Dispatcher.InvokeAsync(() =>
            {
                Tables = groupings.Select(CreateTable).ToList();

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