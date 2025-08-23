using System.Diagnostics;
using Ara3D.IO.StepParser;
using Ara3D.Utils;

namespace Ara3D.IfcGeometry;

public class IfcPropSet
{
    public int Name;
}

public class IfcPropValue
{
    public int Value;
    public int Name;
}

public class IfcPropSetToProp
{
    public int PropSetId;
    public int PropId;
}

public class IfcObjectToPropSet
{
    public int ObjectId;
    public int PropSetId;
}

public class IfcPropData
{
    public List<IfcObjectToPropSet> ObjectToPropSets = [];
    public List<IfcPropSetToProp> PropSetToProps = [];
    public List<IfcPropValue> PropValues = [];
    public List<IfcPropSet> PropSets = [];
    public IndexedSet<string> Strings = new();

    public IfcPropData(StepDocument doc)
    {
        var res = new StepValueResolver(doc);
        var propSetIdToIndex = new Dictionary<int, int>();
        foreach (var val in res.GetDefinitionValues())
        {
            var name = val.GetEntityName();
            var attrs = val.GetEntityAttributesValue().GetElements().ToList();
            if (name is "IFCPROPERTYSET")
            {
                Debug.Assert(attrs.Count == 5);
                var propSetName = attrs[2].AsString();
                var ids = attrs[4].AsIdList();
                foreach (var id in ids)
                {
                    PropSetToProps.Add(new IfcPropSetToProp() { PropSetId = PropSets.Count, PropId = id });
                }

                PropSets.Add(new IfcPropSet { Name = Strings.Add(propSetName) });
            }
            else if (name is "IFCRELDEFINESBYPROPERTIES")
            {
                Debug.Assert(attrs.Count == 6);
                var objectIds = attrs[4].AsIdList();
                var propSetId = attrs[5].AsId();
                foreach (var objectId in objectIds)
                {
                    ObjectToPropSets.Add(new IfcObjectToPropSet() { ObjectId = objectId, PropSetId = propSetId });
                }
            }
            else if (name is "IFCPROPERTYSINGLEVALUE")
            {
                Debug.Assert(attrs.Count == 4);
                var propName = Strings.Add(attrs[0].AsString());
                var propVal = attrs[2];
                var propValStr = Strings.Add(propVal.ToString());
                PropValues.Add(new IfcPropValue { Name = propName, Value = propValStr });
            }
        }

    }

    public long SizeEstimate()
    {
        var stringSizes = Strings.Keys.Sum(x => x.Length + 1);

        return ObjectToPropSets.Count * 8
               + PropSetToProps.Count * 8
               + PropSets.Count * 4
               + PropValues.Count * 8
               + stringSizes;
    }
}