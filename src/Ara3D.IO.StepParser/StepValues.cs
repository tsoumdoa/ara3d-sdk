using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Ara3D.Memory;

namespace Ara3D.IO.StepParser;

public unsafe class StepValues
{
    public UnmanagedList<StepValue> Values = new();
    public UnmanagedList<StepToken> Tokens = new();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepValues(int capacity)
    {
        Values.Accomodate(capacity);
        Tokens.Accomodate(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTokens(ref StepToken* cur, StepToken* end)
    {
        while (cur < end)
        {
            ProcessNextToken(ref cur, end);
            cur++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddId(StepToken token)
        => AddValue(new StepValue(StepKind.Id, ParseId(token)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddTokenAndValue(StepToken token, StepKind kind)
    {
        var id = AddToken(token);
        AddValue(new StepValue(kind, id));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEntity(StepToken token)
        => AddTokenAndValue(token, StepKind.Entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddNumber(StepToken token)
        => AddTokenAndValue(token, StepKind.Number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddString(StepToken token)
        => AddTokenAndValue(token, StepKind.String);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddSymbol(StepToken token)
        => AddTokenAndValue(token, StepKind.Symbol);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddRedeclared()
        => AddValue(new StepValue(StepKind.Redeclared, 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddUnassigned()
        => AddValue(new StepValue(StepKind.Unassigned, 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ParseId(StepToken token)
        => uint.Parse(new ReadOnlySpan<byte>(token.Begin + 1, unchecked((int)(token.End - 1 - token.Begin))));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddValue(StepValue value)
        => Values.Add(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint AddToken(StepToken token)
    {
        Tokens.Add(token);
        return (uint)(Tokens.Count - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString(StepValue value)
        => value.BuildString(this, new StringBuilder()).ToString();
}