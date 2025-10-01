namespace Ara3D.DataTable;

public class DataColumn : IDataColumn
{
    public int ColumnIndex { get; }
    public IDataDescriptor Descriptor { get; }
    public Array Values { get; }
    public int Count => Values.Length;
    public object this[int index] => Values.GetValue(index);
    public DataColumn(Array values, IDataDescriptor descriptor, int index)
    {
        Values = values;
        Descriptor = descriptor;
        ColumnIndex = index;
    }
    public Array AsArray() => Values;

    public override string ToString()
        => $"{Descriptor.Name}:{Descriptor.Type}[{Count}]";
}