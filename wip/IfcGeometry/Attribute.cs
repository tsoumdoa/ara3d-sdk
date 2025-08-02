using System.Runtime.InteropServices;
using System.Xml.Linq;
using Ara3D.IO.StepParser;
using Ara3D.Utils;

namespace Ara3D.IfcGeometry;

public class Attribute;

public class AttributeList : Attribute
{
    public int Count;
    public List<Attribute> Attributes = new();
    public override string ToString() 
        => $"(" + Attributes.JoinStrings(" , ") + ")";
}

public class AttributeEntity : Attribute
{
    public HashSet<string> EntityNames = new();
    public override string ToString() 
        => EntityNames.Count > 1 ? 
            "(" + EntityNames.OrderBy(x => x).JoinStrings(" | ") + ")"
            : EntityNames.JoinStrings();

    public AttributeEntity Add(string name)
    {
        EntityNames.Add(name);
        return this;
    }
}

public class AttributeUnassigned : Attribute
{
    public override string ToString() => "$";
}

public class AttributeRedeclared : Attribute
{
    public override string ToString() => "*";
}

public class AttributeNumber : Attribute
{
    public override string ToString() => "NUMBER";
}

public class AttributeString : Attribute
{
    public override string ToString() => "STRING";
}

public class AttributeSymbol : Attribute
{
    public HashSet<string> Symbols = new();
    public override string ToString() 
        => "(" + Symbols.OrderBy(x => x).Select(x => $".{x}.").JoinStrings(" | ") + ")";
    
    public AttributeSymbol Add(string name)
    {
        Symbols.Add(name);
        return this;
    }
}

public class AttributeArray : Attribute
{
    public HashSet<string> ElementTypes = new();
    public int MinCount = Int32.MaxValue;
    public int MaxCount = Int32.MinValue;
    
    public override string ToString() 
        => (MinCount > MaxCount ? "ARRAY[]" :
            $"ARRAY[{MinCount}..{MaxCount}]")
           + "(" + ElementTypes.OrderBy(x => x).JoinStrings(" | ") + ")";

    public AttributeArray Add(string name, int count)
    {
        ElementTypes.Add(name);
        MinCount = Math.Min(count, MinCount);
        MaxCount = Math.Max(count, MaxCount);
        return this;
    }
}

public class AttributeId : Attribute
{
    public HashSet<string> Entities = new();
    public override string ToString() 
        => "ID(" + Entities.OrderBy(x => x).JoinStrings(",") + ")";

    public AttributeId Add(string name)
    {
        Entities.Add(name);
        return this;
    }
}

public class AttributeUnion : Attribute
{
    public List<string> Attributes = new();
    public override string ToString() 
        => "UNION(" + Attributes.OrderBy(x => x).JoinStrings(",") + ")";
}

public static class AttributeExtensions
{
    public static Dictionary<string, AttributeList> ComputeAttributes(this StepGraph graph)
    {
        var r = new Dictionary<string, AttributeList>();
        foreach (var def in graph.Definitions.Values)
        {
            var name = graph.Data.GetEntityName(def);

            if (!r.TryGetValue(name, out var list))
            {
                list = new AttributeList();
                r.Add(name, list);
            }

            list.Count++;
            graph.UpdateAttribute(def, list);
        }

        return r;
    }

    public static AttributeList UpdateAttribute(this StepGraph graph, StepDefinition def, AttributeList list)
    {
        var data = graph.Data;
        var attrs = data.GetAttributes(def);
        var i = 0;
        var attrIndex = 0;
        while (i < attrs.Length)
        {
            var val = attrs[i];
            var newAttr = CreateAttribute(graph, val);

            if (val.IsList)
            {
                if (val.Count > 0)
                {
                    var innerAttr = CreateAttribute(graph, attrs[i + 1]);
                    if (newAttr is AttributeArray aa)
                        aa.Add(innerAttr.ToString(), val.Count);
                }
                i += val.Count;
            }

            if (attrIndex < list.Attributes.Count)
            {
                var attr = list.Attributes[attrIndex];
                attr.MergeAttribute(newAttr);
            }
            else
            {
                list.Attributes.Add(newAttr);
            }

            i += 1; // Move to the next attribute
            attrIndex++;
        }
        return list;
    }

    public static Attribute MergeAttribute(this Attribute self, Attribute other)
    {
        if (self is AttributeList listSelf && other is AttributeList listOther)
        {
            listSelf.Attributes.AddRange(listOther.Attributes);
            return listSelf;
        }
        else if (self is AttributeEntity entitySelf && other is AttributeEntity entityOther)
        {
            entitySelf.EntityNames.UnionWith(entityOther.EntityNames);
            return entitySelf;
        }
        else if (self is AttributeSymbol symbolSelf && other is AttributeSymbol symbolOther)
        {
            symbolSelf.Symbols.UnionWith(symbolOther.Symbols);
            return symbolSelf;
        }
        else if (self is AttributeArray arraySelf && other is AttributeArray arrayOther)
        {
            arraySelf.ElementTypes.UnionWith(arrayOther.ElementTypes);
            arraySelf.MinCount = Math.Min(arraySelf.MinCount, arrayOther.MinCount);
            arraySelf.MaxCount = Math.Max(arraySelf.MaxCount, arrayOther.MaxCount);
            return arraySelf;
        }
        else if (self is AttributeId idSelf && other is AttributeId idOther)
        {
            idSelf.Entities.UnionWith(idOther.Entities);
            return idSelf;
        }
        else if (self is AttributeUnion unionSelf && other is AttributeUnion unionOther)
        {
            unionSelf.Attributes.AddRange(unionOther.Attributes);
            return unionSelf;
        }

        var r = new AttributeUnion();
        r.Attributes.Add(self.ToString());
        r.Attributes.Add(other.ToString());
        return r;
    }

    public static Attribute CreateAttribute(this StepGraph graph, StepValue val)
    {
        switch (val.Kind)
        {
            case StepKind.Id:
                return new AttributeEntity().Add(
                    graph.GetEntityName(
                        graph.Data.AsId(val)));

            case StepKind.Entity:
                return new AttributeEntity().Add(
                    graph.Data.AsString(val));

            case StepKind.Number:
                return new AttributeNumber();

            case StepKind.List:
                return new AttributeArray();

            case StepKind.Redeclared: 
                return new AttributeRedeclared();

            case StepKind.Unassigned:
                return new AttributeUnassigned();

            case StepKind.Symbol:
                return new AttributeSymbol().Add(
                    graph.Data.AsTrimmedString(val));

            case StepKind.String:
                return new AttributeString();

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}