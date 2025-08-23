using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ara3D.IO.StepParser;

/// <summary>
/// This is a high-level class for working with values in a step file.
/// It is less performant than the StepRawValue struct, but provides a much simpler API. 
/// </summary>
public class StepValue
{
    public readonly StepValueResolver Resolver;
    public readonly StepRawValue RawValue;

    public StepValue(StepValueResolver resolver, StepRawValue rawValue)
    {
        Resolver = resolver;
        RawValue = rawValue;
    }

    public StepKind GetKind()
        => RawValue.Kind;

    public StepKind GetListElementKind()
    {
        if (IsList())
            throw new Exception("Not a list kind");
        var rawVals = RawValueData.AsArray(RawValue);
        if (rawVals.Length == 0)
            return StepKind.Unknown;
        var r = rawVals[0].Kind;
        foreach (var el in rawVals)
        {
            if (el.Kind != r)
                return StepKind.Unknown;
        }
        return r;
    }

    public string GetEntityName()
    {
        if (!IsEntity())
            throw new Exception("Not an entity");
        return RawValueData.GetEntityName(RawValue);
    }

    public IReadOnlyList<StepValue> GetEntityAttributes()
        => GetEntityAttributesValue().GetElements().ToList();

    public StepValue GetEntityAttributesValue()
    {
        if (!IsEntity())
            throw new Exception("Not an entity");
        var attrIndex = RawValue.GetEntityAttributeValueIndex();
        var attr = RawValueData.Values[attrIndex];
        Debug.Assert(attr.IsList);
        return new StepValue(Resolver, attr);
    }

    public string AsString()
        => !IsString()
            ? throw new Exception("Not a string")
            : RawValueData.AsString(RawValue);

    public override string ToString()
        => RawValueData.ToString(RawValue);

    public double AsNumber()
        => RawValueData.AsNumber(RawValue);

    public int AsId()
        => RawValueData.AsId(RawValue);

    public StepValue AsRef()
        => Resolver.Resolve(RawValueData.AsToken(RawValue));

    public IReadOnlyList<double> AsNumberList()
        => GetElements().Select(x => x.AsNumber()).ToList();
    
    public IReadOnlyList<int> AsIdList()
        => GetElements().Select(e => e.AsId()).ToList();

    public IReadOnlyList<StepValue> AsRefList()
        => GetElements().Select(e => e.AsRef()).ToList();

    public IEnumerable<StepValue> GetElements()
    {
        if (!IsList())
            throw new Exception("Not a list");
        var rawArray = RawValueData.AsArray(RawValue);
        var i = 0;
        while (i < rawArray.Length)
        {
            var rawEl = rawArray[i];
            var el = new StepValue(Resolver, rawEl);
            yield return el;
            if (rawEl.IsList)
            {
                i += rawEl.Count + 1;
            }
            else if (rawEl.IsEntity)
            {
                i += 1;
                if (i >= rawArray.Length || !rawArray[i].IsList)
                    throw new Exception("Expected a list to follow an entity");
                i += rawArray[i].Count + 1;
            }
            else
            {
                i++;
            }
        }
    }

    public bool IsNumber()
        => GetKind() == StepKind.Number;

    public bool IsList()
        => GetKind() == StepKind.List;

    public bool IsId()
        => GetKind() == StepKind.Id;

    public bool IsEntity()
        => GetKind() == StepKind.Entity;

    public bool IsString()
        => GetKind() == StepKind.String;

    public bool IsSymbol()
        => GetKind() == StepKind.Symbol;

    public bool IsNumberList()
        => IsList() && GetElements().All(x => x.IsNumber());

    public bool IsRefList()
        => IsList() && GetElements().All(x => x.IsId());

    public StepRawValueData RawValueData 
        => Resolver.RawValueData;
}
