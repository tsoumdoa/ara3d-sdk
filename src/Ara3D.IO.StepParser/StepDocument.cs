using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ara3D.Logging;
using Ara3D.Memory;
using Ara3D.Utils;

namespace Ara3D.IO.StepParser
{
    public sealed unsafe class StepDocument : IDisposable
    {
        public readonly FilePath FilePath;
        public readonly byte* DataStart;
        public readonly byte* DataEnd;
        public readonly AlignedMemory Data;
        public readonly Dictionary<ulong, StepDefinition> Definitions = new();
        public readonly StepValues Values;

        public StepDocument(FilePath filePath, ILogger logger = null)
        {
            FilePath = filePath;
            logger ??= Logger.Null;

            logger.Log($"Loading {filePath.GetFileSizeAsString()} of data from {filePath.GetFileName()}");
            Data = Serializer.ReadAllBytesAligned(filePath);
            DataStart = Data.GetPointer();
            DataEnd = DataStart + Data.NumBytes();

            logger.Log($"Starting tokenization");

            var capacityEstimate = Data.NumBytes / 32;
            Values = new StepValues((int)capacityEstimate);

            // Initialize the token list with a capacity of 16,000 tokens (the longest line we hope to encounter, but could be more)
            using var tokens = new UnmanagedList<StepToken>(32000);

            var cur = DataStart;

            while (true)
            {
                tokens.Clear();
                if (!StepTokenizer.AdvanceToAndTokenizeDefinition(ref cur, DataEnd, out var idToken, tokens))
                    break;

                var id = StepValues.ParseId(idToken);

                //Debug.Assert(!Definitions.ContainsKey(id), $"Duplicate definition found for ID {id} in {filePath.GetFileName()}");
                //Debug.Assert(tokens.Count > 2, "Expected at least 3 tokens for a definition identifier begin_group end_group"));

                //Debug.Assert(tokens[0].Type == StepTokenType.Identifier, "Expected Identifier token at start");
                //Debug.Assert(tokens[1].Type == StepTokenType.BeginGroup, "Expected BeginGroup token at start + 1");
                //Debug.Assert(tokens[^1].Type == StepTokenType.EndGroup, "Expected EndOfLine token at end");

                var curToken = tokens.Begin();
                var endToken = tokens.End();
                var valueIndex = Values.Values.Count;
                Values.AddTokens(ref curToken, endToken);
                //Debug.Assert(curToken == endToken, "Did not consume all tokens in definition");
                
                var definition = new StepDefinition(id, valueIndex, Values);
                Definitions.Add(id, definition);
                
                tokens.Clear();
            }

            logger.Log($"Number of instance definitions = {Definitions.Count}");
            logger.Log($"Completed creation of STEP document from {filePath.GetFileName()}");
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
            Data.Dispose();
        }

        public static StepDocument Create(FilePath fp) 
            => new(fp);

        public IEnumerable<StepDefinition> GetDefinitions()
            => Definitions.Values;
    }
}