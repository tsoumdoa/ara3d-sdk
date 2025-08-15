using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Ara3D.Logging;
using Ara3D.Memory;
using Ara3D.Utils;
using System.Runtime.CompilerServices;

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

    public sealed class U128Comparer : IEqualityComparer<UInt128>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UInt128 x, UInt128 y) => x == y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(UInt128 v)
        {
            // Extract halves; use your own accessors if custom struct
            ulong lo = (ulong)v;
            ulong hi = (ulong)(v >> 64);
            // Mix: xor-shifts + multiply provides good avalanching
            ulong x = lo ^ (hi * 0x9E3779B97F4A7C15UL);
            x ^= x >> 33; x *= 0xff51afd7ed558ccdUL;
            x ^= x >> 33; x *= 0xc4ceb9fe1a85ec53UL;
            x ^= x >> 33;
            return (int)x;
        }

    }
    public Dictionary<StepToken, StepDefinition> GetDefinitionLookup()
    {
        var r = new Dictionary<StepToken, StepDefinition>(Definitions.Count);
        foreach (var def in Definitions)
        {
            r.TryAdd(def.IdToken, def);
        }
        return r;
    }

    public Dictionary<StepToken, string> GetEntityNameLookup()
    {
        var r = new Dictionary<StepToken, string>(Definitions.Count);
        foreach (var def in Definitions)
        {
            r.TryAdd(def.IdToken, ValueData.GetEntityName(def));
        }
        return r;
    }
}