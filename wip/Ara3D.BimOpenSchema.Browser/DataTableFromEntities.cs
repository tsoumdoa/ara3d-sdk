using System.Windows.Data;
using Ara3D.DataTable;
using Ara3D.Utils;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Ara3D.BimOpenSchema.Browser;

public class DataTableFromEntities : IDataTable
{
    public class Row : IDataRow
    {
        public DataTableFromEntities Parent;
        public int RowIndex { get; init; }
        public IDataTable DataTable => Parent;
        public IReadOnlyList<object> Values => Parent.Columns.Select(c => c[RowIndex]).ToList().AsReadOnly();
        public object this[int index] => Parent.Columns[index][RowIndex];

        public Row(DataTableFromEntities parent, int index)
        {
            Parent = parent;
            RowIndex = index;
        }
    }

    public class Column : IDataColumn
    {
        public object DefaultValue { get; }
        public string Name => Descriptor.Name;
        public Type Type => Descriptor.Type;
        public List<object> Values { get; } = new();
        public Array AsArray() => Values.ToArray();
        public int ColumnIndex { get; }
        public IDataDescriptor Descriptor { get; }
        public int Count => Values.Count;
        public object this[int n] => Values[n];

        public Column(string name, Type type, int index)
        {
            DefaultValue = type.GetDefaultValue();
            ColumnIndex = index;
            Descriptor = new DataDescriptor(name, type, index);
        }
    }

    private IReadOnlyList<EntityModel> Entities { get; }
    public Dictionary<string, Column> ColumnLookup { get; } = new();

    public Column AddColumn(string name, Type type)
    {
        // NOTE: this prevents the same column appearing twice when the parameter names are capitalized differently.
        // That should be flagged as a bug, 
        if (ColumnLookup.TryGetValue(name.ToLowerInvariant(), out var column))
            return column;
        var r = new Column(name, type, ColumnLookup.Count);
        ColumnLookup.Add(name.ToLowerInvariant(), r);
        return r;
    }

    public DataTableFromEntities(IReadOnlyList<EntityModel> entities, string name)
    {
        Entities = entities;
        ColumnLookup.Clear();
        var nameColumn = AddColumn("Name", typeof(string));
        var globalIdColumn = AddColumn("GlobalId", typeof(string));
        var localIdColumn = AddColumn("LocalId", typeof(long));
        var documentIndexColumn = AddColumn("DocumentIndex", typeof(int));

        var nonParameterColumnCount = ColumnLookup.Count;

        // Create the parameter columns
        foreach (var e in Entities)
        {
            foreach (var pm in e.Parameters)
            {
                // TODO: temporary. These type can't be converted to Parquet 
                if (pm.Descriptor.ParameterType == ParameterType.Entity
                    || pm.Descriptor.ParameterType == ParameterType.Point)
                    continue;

                var paramType = pm.Descriptor.DotNetType;
                var paramName = pm.Descriptor.Name;
                AddColumn(paramName, paramType);
            }
        }

        foreach (var e in Entities)
        {
            nameColumn.Values.Add(e.Name);
            localIdColumn.Values.Add(e.LocalId);
            globalIdColumn.Values.Add(e.GlobalId);
            documentIndexColumn.Values.Add(e.DocumentIndex);

            // Add values or default values 
            foreach (var column in ColumnLookup.Values)
            {
                // Skip the first columns
                if (column.ColumnIndex < nonParameterColumnCount)
                    continue;

                if (e.ParameterValues.TryGetValue(column.Name, out var val) 
                    && val.GetType() == column.Type)
                {
                    column.Values.Add(val);
                }
                else 
                {
                    column.Values.Add(column.DefaultValue);
                }
            }
        }

        Columns = ColumnLookup.Values.OrderBy(c => c.ColumnIndex).ToList().AsReadOnly();
        Rows = entities
            .Select((_, index) => new Row(this, index))
            .ToList()
            .AsReadOnly();
        Name = name;
    }

    public string Name { get; }
    public IReadOnlyList<IDataRow> Rows { get; }
    public IReadOnlyList<IDataColumn> Columns { get; }

    public object this[int column, int row] => Columns[column][row];
}