using System.Windows;
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
        public BimDataModel Model;
        public IReadOnlyList<IDataTable> Tables;
        public OpenFileDialog OpenFileDialog = null;
        public SaveFileDialog SaveParquetFileDialog = null;
        public SaveFileDialog SaveExcelFileDialog = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        public static IDataTable CreateParameterDataTable(BimDataModel model, string category)
        {
            var entities = model.Entities.Where(e => e.Category == category).ToList();
            return new DataTableFromEntities(entities, category);
        }

        private async void Raw_Click(object sender, RoutedEventArgs e)
        {
            await UpdateTables();
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
                var fp = new FilePath(OpenFileDialog.FileName);
                Model = null;
                Data = await fp.ReadBimDataFromParquetZipAsync().ConfigureAwait(false);
                Model = new BimDataModel(Data);
                await UpdateTables();
            }
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (Tables == null)
            {
                MessageBox.Show("No data loaded", "Error");
                return;
            }

            SaveExcelFileDialog ??= new SaveFileDialog()
            {
                DefaultExt = ".xlsx",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (SaveExcelFileDialog.ShowDialog() == true)
            {
                var fp = new FilePath(SaveExcelFileDialog.FileName);
                Tables.ToDataSet().WriteToExcel(fp);
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

        private async Task UpdateTables()
        {
            var cats = Model.Entities
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            await Dispatcher.InvokeAsync(() =>
            {
                Tables = RawMenuItem.IsChecked
                    ? Data.ToDataSet().Tables
                    : cats.Select(c => CreateParameterDataTable(Model, c)).ToList();

                TabControl.Items.Clear();
                foreach (var t in Tables)
                {
                    var grid = TabControl.AddDataGridTab(t.Name);
                    grid.AssignDataTable(t);
                }
            });

        }
    }
}   