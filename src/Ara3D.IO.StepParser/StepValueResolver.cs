using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ara3D.IO.StepParser;

/// <summary>
/// This class bundles a document with a lookup table to help find entity values.
/// Ids are represented as StepTokens when used as keys.  
/// </summary>
public class StepValueResolver
{
    public readonly StepDocument Document;
    public readonly StepRawValueData RawValueData;
    public Dictionary<StepToken, StepDefinition> Lookup;

    public StepValueResolver(StepDocument document)
    {
        Document = document;
        RawValueData = document.RawValueData;
        Lookup = document.GetDefinitionLookup();
    }

    public IEnumerable<(int, StepValue)> GetDefinitionIdsAndValues()
        => Document.Definitions.Select(def => (def.Id, GetEntityValue(def)));

    public IEnumerable<StepValue> GetDefinitionValues()
        => Document.Definitions.Select(GetEntityValue);

    public StepValue GetEntityValue(StepDefinition def)
        => new StepValue(this, RawValueData.GetEntityValue(def));

    public StepValue Resolve(StepToken token)
    {
        Debug.Assert(token.Type == StepTokenType.Id);
        Debug.Assert(Lookup.ContainsKey(token));
        if (!Lookup.TryGetValue(token, out var def))
            throw new Exception($"Could not find definition for {token}");
        return GetEntityValue(def);
    }
}