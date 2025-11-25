using System.Windows;
using System.Windows.Controls;
using Ara3D.BimOpenSchema.IO;
using Ara3D.DataTable;
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
        public BimObjectModel Model;
        public IReadOnlyList<IDataTable> Tables;
        public Grouping CurrentGrouping = Grouping.None;
        public OpenFileDialog OpenFileDialog = null;
        public SaveFileDialog SaveParquetFileDialog = null;
        public SaveFileDialog SaveExcelFileDialog = null;

        public enum Grouping
        {
            None,
            Category,
            Assembly,
            Level,
            Group,
            Class,
            Room,
            Document,
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

        public async Task OpenFile(FilePath fp)
        {
            Model = null;
            Data = await fp.ReadBimDataFromParquetZipAsync().ConfigureAwait(false);
            Model = new BimObjectModel(Data);
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
                    return Model.Entities.GroupBy(_ => "All");
                case Grouping.Assembly:
                    return Model.Entities.GroupBy(e => e.AssemblyName);
                case Grouping.Category:
                    return Model.Entities.GroupBy(e => e.Category);
                case Grouping.Level:
                    return Model.Entities.GroupBy(e => e.LevelName);
                case Grouping.Group:
                    return Model.Entities.GroupBy(e => e.GroupName);
                case Grouping.Class:
                    return Model.Entities.GroupBy(e => e.ClassName);
                case Grouping.Room:
                    return Model.Entities.GroupBy(e => e.RoomName);
                case Grouping.Document:
                    return Model.Entities.GroupBy(e => e.DocumentTitle);
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

            var inputFile = new FilePath(OpenFileDialog.FileName);
            var folder = inputFile.RelativeFolder(inputFile.GetFileNameWithoutExtension());

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
            => new (entities.ToList(), entities.Key, IncludeParamsMenuItem.IsChecked);

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