using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Ara3D.Memory;

namespace Ara3D.IO.StepParser;

public static unsafe class StepTokenizer
{
    public static readonly StepTokenType[] TokenLookup =
        StepTokenizerLookupHelpers.CreateTokenLookup();

    public static readonly bool[] IsNumberLookup =
        StepTokenizerLookupHelpers.CreateNumberLookup();

    public static readonly bool[] IsIdentLookup =
        StepTokenizerLookupHelpers.CreateIdentLookup();

    public static readonly bool[] IsDigitLookup =
        StepTokenizerLookupHelpers.CreateDigitLookup();

    public static readonly bool[] IsWhiteSpaceLookup =
        StepTokenizerLookupHelpers.CreateWhiteSpaceLookup();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldStoreType(StepTokenType type)
    {
        switch (type)
        {
            case StepTokenType.Identifier:
            case StepTokenType.SingleQuotedString:
            case StepTokenType.DoubleQuotedString:
            case StepTokenType.Number:
            case StepTokenType.Symbol:
            case StepTokenType.Id:
            case StepTokenType.Unassigned:
            case StepTokenType.Redeclared:
                return true;

            case StepTokenType.BeginGroup:
            case StepTokenType.EndGroup:
            case StepTokenType.EndOfLine:
            case StepTokenType.Definition:
                return true;

            case StepTokenType.None:
            case StepTokenType.Whitespace:
            case StepTokenType.Separator:
            case StepTokenType.Comment:
            case StepTokenType.Unknown:
                return false;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StepTokenType ParseToken(ref byte* cur, byte* end)
    {
        Debug.Assert(cur < end);
        switch (*cur++)
        {
            case (byte)'(':
                return StepTokenType.BeginGroup;

            case (byte)')':
                return StepTokenType.EndGroup;

            case (byte)'=':
                return StepTokenType.Definition;
                
            case (byte)';':
                return StepTokenType.EndOfLine;

            case (byte)'$':
                return StepTokenType.EndGroup;

            case (byte)',':
                return StepTokenType.Separator;

            case (byte)'*':
                return StepTokenType.Redeclared;

            case (byte)' ':
            case (byte)'\t':
            case (byte)'\n':
            case (byte)'\r':
                while (cur < end && IsWhiteSpaceLookup[*cur]) cur++;
                return StepTokenType.Whitespace;

            case (byte)'a':
            case (byte)'b':
            case (byte)'c':
            case (byte)'d':
            case (byte)'e':
            case (byte)'f':
            case (byte)'g':
            case (byte)'h':
            case (byte)'i':
            case (byte)'j':
            case (byte)'k':
            case (byte)'l':
            case (byte)'m':
            case (byte)'n':
            case (byte)'o':
            case (byte)'p':
            case (byte)'q':
            case (byte)'r':
            case (byte)'s':
            case (byte)'t':
            case (byte)'u':
            case (byte)'v':
            case (byte)'w':
            case (byte)'x':
            case (byte)'y':
            case (byte)'z':
            case (byte)'A':
            case (byte)'B':
            case (byte)'C':
            case (byte)'D':
            case (byte)'E':
            case (byte)'F':
            case (byte)'G':
            case (byte)'H':
            case (byte)'I':
            case (byte)'J':
            case (byte)'K':
            case (byte)'L':
            case (byte)'M':
            case (byte)'N':
            case (byte)'O':
            case (byte)'P':
            case (byte)'Q':
            case (byte)'R':
            case (byte)'S':
            case (byte)'T':
            case (byte)'U':
            case (byte)'V':
            case (byte)'W':
            case (byte)'X':
            case (byte)'Y':
            case (byte)'Z':
            case (byte)'_':
                while (cur < end && IsIdentLookup[*cur]) cur++;
                return StepTokenType.Identifier;

            case (byte)'\"':
                while (cur < end && *cur != (byte)'"') cur++;
                cur++; // Skip the closing quote
                return StepTokenType.DoubleQuotedString;

            case (byte)'\'': 
                while (cur < end && *cur != (byte)'\'') cur++;
                cur++; // Skip the closing quote
                return StepTokenType.SingleQuotedString;

            case (byte)'0':
            case (byte)'1':
            case (byte)'2':
            case (byte)'3':
            case (byte)'4':
            case (byte)'5':
            case (byte)'6':
            case (byte)'7':
            case (byte)'8':
            case (byte)'9':
                while (cur < end && IsNumberLookup[*cur]) cur++;
                return StepTokenType.Number;

            case (byte)'.':
                while (cur < end && IsIdentLookup[*cur]) cur++;
                cur++; // Skip the closing '.'
                return StepTokenType.Symbol;

            case (byte)'#':
                while (cur < end && IsDigitLookup[*cur]) cur++;
                return StepTokenType.Id;

            case (byte)'/':
                var prev = *cur++;
                while (cur < end && (prev != '*' || *cur != '/'))
                    prev = *cur++;
                cur++;
                return StepTokenType.Comment;

            default:
                return StepTokenType.Unknown;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhiteSpace(StepTokenType type)
    {
        return type == StepTokenType.Whitespace || type == StepTokenType.Comment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AdvancePast(ref byte* cur, byte* end, StepTokenType type)
    {
        if (cur >= end) return false;
        var r = ParseToken(ref cur, end);
        while (IsWhiteSpace(r))
        {
            if (cur >= end) return false;
            r = ParseToken(ref cur, end);
        }
        return r == type;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AdvanceTo(ref byte* cur, byte* end, out StepToken token, StepTokenType type)
    {
        while (cur < end)
        {
            var begin = cur;
            var r = ParseToken(ref cur, end);
            if (r == type)
            {
                token = new StepToken(begin, cur);
                return true;
            }
        }
        token = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AdvanceToDefinition(ref byte* cur, byte* end, out StepToken id)
    {
        id = default;
        while (cur < end)
        {
            if (!AdvanceTo(ref cur, end, out id, StepTokenType.Id))
                return false;
            if (AdvancePast(ref cur, end, StepTokenType.Definition))
                return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AdvanceToAndTokenizeDefinition(ref byte* cur, byte* end, out StepToken id, UnmanagedList<StepToken> tokens)
    {
        Debug.Assert(tokens.Count == 0);

        if (!AdvanceToDefinition(ref cur, end, out id))
            return false;
            
        while (cur < end)
        {
            var begin = cur;
            var type = ParseToken(ref cur, end);
            if (type == StepTokenType.EndOfLine)
                return true;
            if (ShouldStoreType(type))
                tokens.Add(new StepToken(begin, cur));
        }

        return false;
    }
}