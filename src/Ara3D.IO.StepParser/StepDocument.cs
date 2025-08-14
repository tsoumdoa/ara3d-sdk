using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Ara3D.Logging;
using Ara3D.Memory;
using Ara3D.Utils;

namespace Ara3D.IO.StepParser;

public sealed unsafe class StepDocument : IDisposable
{
    public readonly FilePath FilePath;
    public readonly byte* DataStart;
    public readonly byte* DataEnd;
    public readonly IBuffer Data;
    public readonly UnmanagedList<StepDefinition> Definitions = new();
    public readonly StepValueData ValueData;

    public StepDocument(FilePath filePath, ILogger logger = null)
        : this(Serializer.ReadAllBytesAligned(filePath), filePath, logger)
    { }

    public StepDocument(IBuffer data, string filePath = "", ILogger logger = null)
    {
        FilePath = filePath;
        logger ??= Logger.Null;
        Data = data;
        DataStart = Data.GetPointer();
        DataEnd = DataStart + Data.NumBytes();

        logger.Log($"Starting tokenization");

        var capacityEstimate = Data.NumBytes() / 32;
        ValueData = new StepValueData((int)capacityEstimate);
        var estNumDefs = capacityEstimate / 8; // Estimate about 8 tokens per definition on average
        Definitions = new UnmanagedList<StepDefinition>((int)estNumDefs);

        // Initialize the token list with a capacity of 16,000 tokens (the longest line we hope to encounter, but could be more)
        using var tokens = new UnmanagedList<StepToken>(32000);

        var cur = DataStart;

        while (true)
        {
            tokens.Clear();
            if (!StepTokenizer.AdvanceToAndTokenizeDefinition(ref cur, DataEnd, out var idToken, tokens))
                break;

            //var id = StepValues.ParseId(idToken);

            //Debug.Assert(!Definitions.ContainsKey(id), $"Duplicate definition found for ID {id} in {filePath.GetFileName()}");
            //Debug.Assert(tokens.Count > 2, "Expected at least 3 tokens for a definition identifier begin_group end_group"));
            //Debug.Assert(tokens[0].Type == StepTokenType.Identifier, "Expected Identifier token at start");
            //Debug.Assert(tokens[1].Type == StepTokenType.BeginGroup, "Expected BeginGroup token at start + 1");
            //Debug.Assert(tokens[^1].Type == StepTokenType.EndGroup, "Expected EndOfLine token at end");

            var curToken = tokens.Begin();
            var endToken = tokens.End();
            var valueIndex = ValueData.Values.Count;
            ValueData.AddTokens(ref curToken, endToken);
            //Debug.Assert(curToken == endToken, "Did not consume all tokens in definition");
                
            var definition = new StepDefinition(idToken, valueIndex);
            Definitions.Add(definition);
                
            tokens.Clear();
        }

        logger.Log($"Number of instance definitions = {Definitions.Count}");
    }

    public static bool Assert(bool condition, string text)
    {
        if (!condition)
            throw new Exception($"Assertion failed: {text}");
        return true;
    }
        
    public void Dispose()
    {
        Trace.WriteLine($"Disposing data");
        if (Data is IDisposable d)
            d.Dispose();
    }

    public static StepDocument Create(FilePath fp) 
        => new(fp);

    public Dictionary<UInt128, StepDefinition> GetDefinitionLookup() 
        => Definitions.ToDictionary(def => def.Id, def => def);

    public Dictionary<UInt128, string> GetEntityNameLookup()
        => Definitions.ToDictionary(def => def.Id, ValueData.GetEntityName);
}