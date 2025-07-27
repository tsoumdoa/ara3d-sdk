using System;
using System.Collections.Generic;
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
        public readonly StepValues Values = new();

        public StepDocument(FilePath filePath, ILogger logger = null)
        {
            FilePath = filePath;
            logger ??= Logger.Null;

            logger.Log($"Loading {filePath.GetFileSizeAsString()} of data from {filePath.GetFileName()}");
            Data = Serializer.ReadAllBytesAligned(filePath);
            DataStart = Data.GetPointer();
            DataEnd = DataStart + Data.NumBytes();

            logger.Log($"Starting tokenization");

            // Initialize the token list with a capacity of 16,000 tokens (the longest line we hope to encounter, but could be more)
            using var tokens = new UnmanagedList<StepToken>(16000);

            var cur = DataStart;

            while (true)
            {
                tokens.Clear();
                if (!StepTokenizer.AdvanceToAndTokenizeDefinition(ref cur, DataEnd, out var idToken, tokens))
                    break;

                var id = StepValues.ParseId(idToken);
                if (!Assert(!Definitions.ContainsKey(id), $"Duplicate definition found for ID {id} in {filePath.GetFileName()}"))
                    continue;

                if (!Assert(tokens.Count > 2, "Expected at least 3 tokens for a definition identifier begin_group end_group"))
                    continue;

                if (!Assert(tokens[0].Type == StepTokenType.Identifier, "Expected Identifier token at start")) continue;
                if (!Assert(tokens[1].Type == StepTokenType.BeginGroup, "Expected BeginGroup token at start + 1")) continue;
                if (!Assert(tokens[^1].Type == StepTokenType.EndGroup, "Expected EndOfLine token at end")) continue;

                var curToken = tokens.Begin();
                var endToken = tokens.End();
                var valueIndex = Values.Values.Count;
                Values.AddTokens(ref curToken, endToken);
                if (!Assert(curToken == endToken, "Did not consume all tokens in definition")) continue;
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
            Data.Dispose();
            Data.Dispose();
        }

        public static StepDocument Create(FilePath fp) 
            => new(fp);

        public IEnumerable<StepDefinition> GetDefinitions()
            => Definitions.Values;
    }
}