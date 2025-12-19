using System.Collections;
using System.Reflection;
using Ara3D.Collections;
using Ara3D.DataTable;
using Ara3D.PropKit;
using Ara3D.Utils;
using DataColumn = Parquet.Data.DataColumn;

namespace Ara3D.Extras;

public class ParquetTable<T> : IReadOnlyList<T>, IDataTable
{
    public string Name { get; }
    public IReadOnlyList<IDataRow> Rows { get;  }
    public IReadOnlyList<IDataColumn> Columns { get; }
    public object this[int column, int row] => GetRow(row).Values[column];
    private IReadOnlyList<DataColumn> _columns { get; }
    private readonly IPropAccessor<T>[] _accessors;
    private readonly Func<T> _ctor;
    
    public ParquetTable(string name, IReadOnlyList<DataColumn> columns)
    {
        _columns = columns;
        Name = name;
        Count = _columns.Count > 0 ? _columns[0].NumValues : 0;

        var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fields.Length != _columns.Count)
            throw new InvalidOperationException($"Field count ({fields.Length}) != column count ({_columns.Count}).");

        _ctor = ReflectionUtils.CreateCtor<T>();
        _accessors = typeof(T).GetPropAccessors().Cast<IPropAccessor<T>>().ToArray();
        
        Rows = new ReadOnlyList<IDataRow>(Count, GetRow);
        Columns = _columns.Select((c, i) => new ParquetColumnAdapter(c, i)).ToList();
    }

    public IDataRow GetRow(int n)
        => new DataRow(this, n);

    public T this[int n]
    {
        get
        {
            var obj = _ctor();
            for (int i = 0; i < _columns.Count; i++)
            {
                _accessors[i].SetValue(ref obj, _columns[i].Data.GetValue(n));
            }

            return obj;
        }
    }

    public int Count { get; }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public override string ToString()
        => $"Table {Name}, {Columns.Count} Columns, {Count} Rows";
}