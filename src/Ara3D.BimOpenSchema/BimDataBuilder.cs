using System.Collections.Generic;
using System.Diagnostics;

namespace Ara3D.BimOpenSchema;

// This is a helper class for incrementally constructing a BIMData object without repeating objects. 
public class BimDataBuilder : IBimData
{
    private readonly Dictionary<Entity, int> _entityLookup = new();
    private readonly Dictionary<Document, int> _documentLookup = new();
    private readonly Dictionary<Point, int> _pointLookup = new();
    private readonly Dictionary<ParameterDescriptor, int> _descriptorLookup = new();
    private readonly Dictionary<string, int> _stringLookup = new();

    private readonly List<ParameterDescriptor> _descriptors = [];
    private readonly List<ParameterInt> _integerParameters = [];
    private readonly List<ParameterSingle> _singleParameters = [];
    private readonly List<ParameterString> _stringParameters = [];
    private readonly List<ParameterEntity> _entityParameters = [];
    private readonly List<ParameterPoint> _pointParameters = [];
    private readonly List<Document> _documents = [];
    private readonly List<Entity> _entities = [];
    private readonly List<string> _strings = [];
    private readonly List<Point> _points = [];
    private readonly List<EntityRelation> _relations = [];

    public IReadOnlyList<ParameterDescriptor> Descriptors => _descriptors;
    public IReadOnlyList<ParameterInt> IntegerParameters => _integerParameters;
    public IReadOnlyList<ParameterSingle> SingleParameters => _singleParameters;
    public IReadOnlyList<ParameterString> StringParameters => _stringParameters;
    public IReadOnlyList<ParameterEntity> EntityParameters => _entityParameters;
    public IReadOnlyList<ParameterPoint> PointParameters => _pointParameters;
    public IReadOnlyList<Document> Documents => _documents;
    public IReadOnlyList<Entity> Entities => _entities;
    public IReadOnlyList<string> Strings => _strings;
    public IReadOnlyList<Point> Points => _points;
    public IReadOnlyList<EntityRelation> Relations => _relations;

    public BimGeometry Geometry { get; set; }

    private int Add<T>(Dictionary<T, int> d, List<T> list, T val)
    {
        Debug.Assert(val != null);
        if (val == null)
            Debugger.Break();
        if (d.TryGetValue(val, out var index))
            return index;
        var r = d.Count;
        d.Add(val, r);
        list.Add(val);
        Debug.Assert(d.Count == list.Count);
        return r;
    }
    
    public void AddRelation(EntityIndex a, EntityIndex b, RelationType rt)
        => _relations.Add(new(a, b, rt));

    public EntityIndex AddEntity(long localId, string globalId, DocumentIndex d, string name, string category)
        => (EntityIndex)Add(_entityLookup, _entities, new(localId, AddString(globalId), d, AddString(name), AddString(category)));

    public DocumentIndex AddDocument(string title, string pathName)
        => (DocumentIndex)Add(_documentLookup, _documents, new(AddString(title), AddString(pathName)));

    public PointIndex AddPoint(Point p)
        => (PointIndex)Add(_pointLookup, _points, p);

    public DescriptorIndex AddDescriptor(string name, string units, string group, ParameterType pt)
        => (DescriptorIndex)Add(_descriptorLookup, _descriptors, new(AddString(name), AddString(units), AddString(group), pt));

    public StringIndex AddString(string name)
        => (StringIndex)Add(_stringLookup, _strings, name ?? "");

    public void AddParameter(EntityIndex e, double val, DescriptorIndex d)
        => _singleParameters.Add(new(e, d, (float)val));

    public void AddParameter(EntityIndex e, int val, DescriptorIndex d)
        => _integerParameters.Add(new(e, d, val));

    public void AddParameter(EntityIndex e, EntityIndex val, DescriptorIndex d)
        => _entityParameters.Add(new(e, d, val));

    public void AddParameter(EntityIndex e, string val, DescriptorIndex d)
        => _stringParameters.Add(new(e, d, AddString(val)));

    public void AddParameter(EntityIndex e, PointIndex pi, DescriptorIndex d)
        => _pointParameters.Add(new(e, d, pi));

    public void AddParameter(EntityIndex e, Point p, DescriptorIndex d)
        => _pointParameters.Add(new(e, d, AddPoint(p)));

    public void AddParameter(EntityIndex e, double val, string name, string units, string group)
        => AddParameter(e, val, AddDescriptor(name, units, group, ParameterType.Number));

    public void AddParameter(EntityIndex e, int val, string name, string units, string group)
        => AddParameter(e, val, AddDescriptor(name, units, group, ParameterType.Int));

    public void AddParameter(EntityIndex e, EntityIndex val, string name, string units, string group)
        => AddParameter(e, val, AddDescriptor(name, units, group, ParameterType.Entity));

    public void AddParameter(EntityIndex e, string val, string name, string units, string group)
        => AddParameter(e, val, AddDescriptor(name, units, group, ParameterType.String));

    public void AddParameter(EntityIndex e, Point p, string name, string units, string group)
        => AddParameter(e, p, AddDescriptor(name, units, group, ParameterType.Point));

    public void AddParameter(EntityIndex e, PointIndex pi, string name, string units, string group)
        => AddParameter(e, pi, AddDescriptor(name, units, group, ParameterType.Int));
}

