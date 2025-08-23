using System.Diagnostics;
using System.Numerics;
using Ara3D.IO.StepParser;
using Ara3D.Utils;

namespace Ara3D.IfcGeometry;

public class IfcBin
{
    public enum Kind
    {
        String, Id, Number, Entity, Symbol, List, Unassigned, Redeclared, Nothing, Vector2, Vector3, Vector4, Triangle
        // NOTE: every IfcFaceOuterBound is really an IfcPolyLoop
    }

    public struct List
    {
        public Kind Kind;
        public int Offset;
        public int Count;
    }

    public struct Var
    {
        public Kind Kind;
        public int Index;
    }

    public List<int> DefinitionIds = [];
    public List<int> DefinitionEntities = [];
    public List<int> DefinitionVars = [];

    public List<string> Strings = [];
    public List<int> Ids = [];
    public List<double> Numbers = [];
    public List<string> EntityNames = [];
    public List<string> Symbols = [];

    public List<int> StringListValues = [];
    public List<int> IdListValues = [];
    public List<int> NumberListValues = [];
    public List<int> EntityNameListValues = [];    
    public List<int> SymbolListValues = [];
    
    public List<List> Lists = [];
    public List<Var> Vars = [];

    public List<Vector2> Vector2s = [];
    public List<Vector3> Vector3s = [];
    public List<Vector4> Vector4s = [];
    public List<int> Triangles = [];

    public static Kind GetKind(StepRawValue val)
    {
        if (val.IsString)
            return Kind.String;
        if (val.IsId)
            return Kind.Id;
        if (val.IsNumber)
            return Kind.Number;
        if (val.IsEntity)
            return Kind.Entity;
        if (val.IsSymbol)
            return Kind.Symbol;
        if (val.IsUnassigned)
            return Kind.Unassigned;
        if (val.IsRedeclared)
            return Kind.Redeclared;
        if (val.IsList)
            return Kind.List;
        throw new Exception("Unsupported kind");
    }

    public static IfcBin Create(StepDocument doc)
    {
        var r = new IfcBin();

        var entities = new IndexedSet<string>();
        var ids = new IndexedSet<StepToken>();
        var numbers = new IndexedSet<double>();
        var strings = new IndexedSet<string>();
        var symbols = new IndexedSet<string>();
        var vector2s = new IndexedSet<Vector2>();
        var vector3s = new IndexedSet<Vector3>();
        var vector4s = new IndexedSet<Vector4>();

        var data = doc.RawValueData;

        int ProcessList(StepRawValue val)
        {
            var list = new List
            {
                Kind = Kind.Nothing,
                Offset = 0,
                Count = 0
            };

            var vals = data.AsArray(val);
            if (vals.Length > 0)
            {
                list.Count = vals.Length;
                list.Kind = GetKind(vals[0]);
                if (list.Kind != Kind.List && vals.All(v => GetKind(v) == list.Kind))
                {
                    // Create a list of the appropriate kind
                    switch (list.Kind)
                    {
                        case Kind.String:
                            list.Offset = r.StringListValues.Count;
                            foreach (var el in vals)
                                r.StringListValues.Add(strings.Add(data.AsString(el)));
                            break;
                        case Kind.Id:
                            list.Offset = r.IdListValues.Count;
                            foreach (var el in vals)
                                r.IdListValues.Add(ids.Add(data.AsToken(el)));
                            break;
                        case Kind.Number:
                            if (list.Count == 2)
                            {
                                vector2s.Add(new Vector2((float)data.AsNumber(vals[0]), (float)data.AsNumber(vals[1])));
                            }
                            else if (list.Count == 3)
                            {
                                vector3s.Add(new Vector3((float)data.AsNumber(vals[0]), (float)data.AsNumber(vals[1]), (float)data.AsNumber(vals[2])));
                            }
                            else if (list.Count == 4)
                            {
                                vector4s.Add(new Vector4((float)data.AsNumber(vals[0]), (float)data.AsNumber(vals[1]), (float)data.AsNumber(vals[2]), (float)data.AsNumber(vals[3])));
                            }
                            else
                            {
                                list.Offset = r.NumberListValues.Count;
                                foreach (var el in vals)
                                    r.NumberListValues.Add(numbers.Add(data.AsNumber(el)));
                            }
                            break;
                        case Kind.Entity:
                            list.Offset = r.EntityNameListValues.Count;
                            foreach (var el in vals)
                                r.EntityNameListValues.Add(entities.Add(data.AsString(el)));
                            break;
                        case Kind.Symbol:
                            list.Offset = r.SymbolListValues.Count;
                            foreach (var el in vals)
                                r.SymbolListValues.Add(symbols.Add(data.AsString(el)));
                            break;
                       case Kind.Unassigned:
                            break;
                        case Kind.Redeclared:
                            break;
                        case Kind.Nothing:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // We are dealing with a var list ...
                    list.Offset = r.Vars.Count;
                    foreach (var el in vals)
                    {
                        var v = new Var
                        {
                            Kind = GetKind(el),
                            Index = ProcessVal(el)
                        };
                        r.Vars.Add(v);
                    }
                }
            }

            r.Lists.Add(list);
            return r.Lists.Count - 1;
        }

        int ProcessVal(StepRawValue val)
        {
            switch (val.Kind)
            {
                case StepKind.Id:
                    return ids.Add(data.AsToken(val));
                case StepKind.Entity:
                    return entities.Add(data.AsString(val));
                case StepKind.Number:
                    return numbers.Add(data.AsNumber(val));
                case StepKind.List:
                    return ProcessList(val);
                case StepKind.Redeclared:
                    return -2;
                case StepKind.Unassigned:
                    return -1;
                case StepKind.Symbol:
                    return symbols.Add(data.AsString(val));
                case StepKind.String:
                    return strings.Add(data.AsString(val));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        foreach (var def in doc.Definitions)
        {
            var defId = def.IdToken;
            var defName = data.GetEntityName(def);
            var defVal = data.GetEntityAttributesValue(def);
            Debug.Assert(defVal.IsList);

            if (defName == "IFCFACEOUTERBOUND" || defName == "IFCPOLYLOOP")
            {
                // skip it.
            }
            else if (defName == "IFCCARTESIANPOINT")
            {
                ProcessVal(defVal);
            }
            else
            {
                r.DefinitionIds.Add(ids.Add(defId));
                r.DefinitionEntities.Add(entities.Add(defName));
                r.DefinitionVars.Add(ProcessVal(defVal));
            }
        }

        r.Strings = strings.OrderedMembers().ToList();
        r.Numbers = numbers.OrderedMembers().ToList();
        r.Ids = ids.OrderedMembers().Select(tkn => tkn.AsId()).ToList();
        r.EntityNames = entities.OrderedMembers().ToList();
        r.Symbols = symbols.OrderedMembers().ToList();
        r.Vector2s = vector2s.OrderedMembers().ToList();
        r.Vector3s = vector3s.OrderedMembers().ToList();
        r.Vector4s = vector4s.OrderedMembers().ToList();

        return r;
    }
}