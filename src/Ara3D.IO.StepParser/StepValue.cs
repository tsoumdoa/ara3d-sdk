using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ara3D.Memory;

namespace Ara3D.IO.StepParser;

public enum StepKind 
{
    Id = 0,
    Entity = 1,
    Number = 2,
    List = 3,
    Redeclared = 4,
    Unassigned = 5,
    Symbol = 6,
    String = 7,
}

public readonly struct StepValue
{
    public StepValue(StepKind kind, ulong value)
    {
        Data = ((ulong)kind & 0xF) | (value << 4);
    }

    public readonly ulong Data;
    public StepKind Kind => (StepKind)(Data & 0xF);
    public ulong Value => Data >> 4;

    public StringBuilder BuildString(StepValues values, StringBuilder sb)
    {
        return Kind switch
        {
            StepKind.Id => sb.Append(Value.ToString()),
            StepKind.Entity => sb.Append(values.Tokens[(int)Value]),
            StepKind.Number => sb.Append(values.Tokens[(int)Value]),
            StepKind.List => BuildStringFromList(values, sb),
            StepKind.Redeclared => sb.Append("*"),
            StepKind.Unassigned => sb.Append("$"),
            StepKind.Symbol => sb.Append(values.Tokens[(int)Value]),
            StepKind.String => sb.Append(values.Tokens[(int)Value]),
            _ => sb.Append("_UNKNOWN_"),
        };
    }

    public StringBuilder BuildStringFromList(StepValues values, StringBuilder sb)
    {
        var vals = GetListValues(values);
        foreach (var list in vals)
        {
            if (sb.Length > 0)
                sb.Append(", ");
            list.BuildString(values, sb);
        }
        return sb;
    }

    public StepValue[] GetListValues(StepValues values)
    {
        Debug.Assert(Kind == StepKind.List);
        DecodeIndexAndCount(Value, out var index, out var count);
        var r = new StepValue[count];
        for (var i=0; i < count; ++i)
            r[i] = values.Values[i + (int)index];
        return r;
    }

    public static ulong EncodeIndexAndCount(uint index, uint count)
    {
        // Count must be under 2^28, since we store it in the lower 28 bits of a long.
        Debug.Assert(count < (uint.MaxValue >> 4));
        return ((ulong)index << 32) | (count << 4);
    }

    public static void DecodeIndexAndCount(ulong encoded, out uint index, out uint count)
    {
        index = (uint)(encoded >> 32);
        count = (uint)((encoded & 0xFFFFFFFF) >> 4);
    }
}

public unsafe class StepValues
{
    public List<StepValue> Values = new();
    public List<StepToken> Tokens = new();

    public void AddTokens(ref StepToken* cur, StepToken* end)
    {
        while (cur < end)
        {
            ProcessNextToken(ref cur, end);
            cur++;
        }
    }

    public void ProcessNextToken(ref StepToken* cur, StepToken* end)
    {
        switch (cur->Type)
        {
            case StepTokenType.Identifier:
                AddEntity(*cur);
                break;

            case StepTokenType.SingleQuotedString:
            case StepTokenType.DoubleQuotedString:
                AddString(*cur);
                break;

            case StepTokenType.Number:
                AddNumber(*cur);
                break;

            case StepTokenType.Symbol:
                AddSymbol(*cur);
                break;

            case StepTokenType.Id:
                AddId(*cur);
                break;
            
            case StepTokenType.Unassigned:
                AddUnassigned();
                break;

            case StepTokenType.Redeclared:
                AddRedeclared();
                break;

            case StepTokenType.BeginGroup:
                AddList(ref cur, end);
                if (cur == end || cur->Type != StepTokenType.EndGroup)
                    throw new Exception("Expected EndGroup token after BeginGroup");
                break;

            case StepTokenType.None:
            case StepTokenType.Whitespace:
            case StepTokenType.Separator:
            case StepTokenType.Comment:
            case StepTokenType.Unknown:
            case StepTokenType.EndGroup:
            case StepTokenType.EndOfLine:
            case StepTokenType.Definition:
                throw new Exception($"Unhandled token type {cur->Type}");
            
            default:
                throw new Exception($"Out of range token type {cur->Type}");
        }
    }

    private void AddId(StepToken token)
        => AddValue(new StepValue(StepKind.Id, ParseId(token)));

    private void AddTokenAndValue(StepToken token, StepKind kind)
    {
        var id = AddToken(token);
        AddValue(new StepValue(kind, id));
    }

    private void AddEntity(StepToken token)
        => AddTokenAndValue(token, StepKind.Entity);

    private void AddNumber(StepToken token)
        => AddTokenAndValue(token, StepKind.Number);

    private void AddString(StepToken token)
        => AddTokenAndValue(token, StepKind.String);

    private void AddSymbol(StepToken token)
        => AddTokenAndValue(token, StepKind.Symbol);

    private void AddList(ref StepToken* cur, StepToken* end)
    {
        // Advance past the begin list 
        cur++;

        AddValue(new StepValue(StepKind.List, 0));
        var curIndex = Values.Count;

        while (cur != end && cur->Type != StepTokenType.EndGroup)
        {
            ProcessNextToken(ref cur, end);
            cur++;
        }

        var listCount = Values.Count - curIndex;
        var val = StepValue.EncodeIndexAndCount((uint)curIndex, (uint)listCount);
        Values[curIndex-1] = new StepValue(StepKind.List, val);
    }

    private void AddRedeclared()
        => AddValue(new StepValue(StepKind.Redeclared, 0));

    private void AddUnassigned()
        => AddValue(new StepValue(StepKind.Unassigned, 0));

    public static ulong ParseId(StepToken token)
    {
        Debug.Assert(token.Type == StepTokenType.Id);
        var span = token.Slice;
        Debug.Assert(span.Length >= 2);
        Debug.Assert(span.First() == '#');
        var id = 0UL;
        for (var i = 1; i < span.Length; ++i)
        {
            Debug.Assert(span[i] >= '0' && span[i] <= '9');
            id = id * 10 + span[i] - '0';
        }
        return id;
    }

    private void AddValue(StepValue value)
        => Values.Add(value);

    private uint AddToken(StepToken token)
    {
        Tokens.Add(token);
        return (uint)(Tokens.Count - 1);
    }

    public string GetString(StepValue value)
        => value.BuildString(this, new StringBuilder()).ToString();
}