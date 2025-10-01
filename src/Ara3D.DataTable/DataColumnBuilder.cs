using System.Collections;

namespace Ara3D.DataTable;

public class DataColumnBuilder : IDataColumn
{
    public int ColumnIndex { get; }
    public IDataDescriptor Descriptor { get; }
    public ArrayList Values { get; }
    public int Count => Values.Count;

    public void Add(object value)
    {
        if (value.GetType() != Descriptor.Type)
            throw new Exception($"Type mismatch expected {Descriptor.Type} byt got {value.GetType()}");
        Values.Add(value);
    }
    public object this[int index] => Values[index];
    public DataColumnBuilder(IDataDescriptor descriptor, int index)
    {
        Values = new ArrayList();
        Descriptor = descriptor;
        ColumnIndex = index;
    }

    public Array AsArray()
    {
        var r = Array.CreateInstance(Descriptor.Type, Values.Count);
        Values.CopyTo(r, 0);
        return r;
    }

    public override string ToString()
        => $"{Descriptor.Name}:{Descriptor.Type}[{Count}]";
}