using System.Diagnostics;
using Ara3D.Collections;

namespace Ara3D.DataTable
{
    public class DataTableBuilder : IDataTable
    {
        public DataTableBuilder(string tableName)
            => Name = tableName;

        public string Name { get; }
        private int _NumRows => Columns.Count > 0 ? Columns[0].Count : 0;
        public IReadOnlyList<IDataRow> Rows => _NumRows.Select(this.GetRow);
        public IReadOnlyList<IDataColumn> Columns => _columns;
        private List<IDataColumn> _columns { get; } = new();

        public DataColumnBuilder AddColumn<T>(string name)
            => AddColumn(name, typeof(T));

        public IDataColumn AddColumn(IDataColumn col)
        {
            _columns.Add(col);
            return col;
        }

        public IDataColumn AddColumn<T>(T[] values, string name)
            => AddColumn(values, name, typeof(T));

        public IDataColumn AddColumn(Array values, string name, Type type)
        {
            var descriptor = new DataDescriptor(name, type, Columns.Count);
            return AddColumn(new DataColumn(values, descriptor, descriptor.Index));
        }

        public DataColumnBuilder AddColumn(string name, Type type)
        {
            var descriptor = new DataDescriptor(name, type, Columns.Count);
            var r = new DataColumnBuilder(descriptor, descriptor.Index);
            _columns.Add(r);
            return r;
        }

        public void AddRow(params object[] values)
            => AddRow((IReadOnlyList<object>)values);

        public void AddRow(IReadOnlyList<object> values)
        {
            if (values.Count != Columns.Count)
                throw new Exception($"Number of value {values.Count} does not match number of columns {Columns.Count}");

            for (var i = 0; i < Columns.Count; i++)
            {
                if (Columns[i] is not DataColumnBuilder columnBuilder)
                    throw new Exception($"Column {i} is not a DataColumnBuilder");
                columnBuilder.Add(values[i]);
            }
        }

        public object this[int column, int row] 
            => Columns[column][row];
    }
}
