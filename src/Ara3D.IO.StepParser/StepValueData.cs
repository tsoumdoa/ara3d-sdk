using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Ara3D.Memory;

namespace Ara3D.IO.StepParser;

/// <summary>
/// Acts as a factory for values and tokens, stores the data associated with them and provides methods to access the data.
/// This allows tokens and values to stay as simple unmanaged types of a fixed length which greatly enhances performance.   
/// </summary>
public unsafe class StepValueData
{
    public UnmanagedList<StepValue> Values = new();
    public UnmanagedList<StepToken> Tokens = new();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepValueData(int capacity)
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
        => AddTokenAndValue(token, StepKind.Id);

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
    private void AddTokenAndValue(StepToken token, StepKind kind)
    {
        Debug.Assert(kind is StepKind.Entity or StepKind.Id or StepKind.Number or StepKind.String or StepKind.Symbol);
        var id = AddToken(token);
        AddValue(new StepValue(kind, id));
    }

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
        Values[curIndex-1] = new StepValue(StepKind.List, curIndex, listCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddRedeclared()
        => AddValue(new StepValue(StepKind.Redeclared, 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddUnassigned()
        => AddValue(new StepValue(StepKind.Unassigned, 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddValue(StepValue value)
        => Values.Add(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddToken(StepToken token)
    {
        Tokens.Add(token);
        return Tokens.Count - 1;
    }

    //==
    // StepDefinition data accessor methods 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetEntityName(StepDefinition def)
        => Tokens[Values[def.ValueIndex].Index].ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepValue GetEntityValue(StepDefinition def)
    {
        var r = Values[def.ValueIndex];
        Debug.Assert(r.Kind == StepKind.Entity);
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepValue GetAttributesValue(StepDefinition def)
    {
        var r = Values[def.ValueIndex + 1];
        Debug.Assert(r.Kind == StepKind.List);
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepValue[] GetAttributes(StepDefinition def)
        => AsArray(GetAttributesValue(def));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(StepDefinition def)
        => $"{GetEntityName(def)}{ToString(GetAttributesValue(def))}";

    //== 
    // String building methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(StepValue value)
        => BuildString(value, new StringBuilder()).ToString();

    public StringBuilder BuildString(StepValue value, StringBuilder sb)
    {
        return value.Kind switch
        {
            StepKind.Id => sb.Append(Tokens[value.Index]),
            StepKind.Entity => sb.Append(Tokens[value.Index]),
            StepKind.Number => sb.Append(Tokens[value.Index]),
            StepKind.List => BuildStringFromList(value, sb),
            StepKind.Symbol => sb.Append(Tokens[value.Index]),
            StepKind.String => sb.Append(Tokens[value.Index]),
            StepKind.Redeclared => sb.Append("*"),
            StepKind.Unassigned => sb.Append("$"),
            _ => sb.Append("_UNKNOWN_"),
        };
    }

    public StringBuilder BuildStringFromList(StepValue value, StringBuilder sb)
    {
        var vals = AsArray(value);
        sb.Append('(');

        var index = 0;
        while (index < vals.Length)
        {
            var val = vals[index];

            BuildString(val, sb);
            if (val.Kind == StepKind.List)
            {
                index += val.Count + 1;
            }
            else
                index++;

            if (index >= vals.Length)
                break;

            if (val.Kind != StepKind.Entity)
                sb.Append(", ");
        }

        sb.Append(')');
        return sb;
    }

    //==
    // StepValue methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepToken AsToken(StepValue value)
    {
        Debug.Assert(value.Kind is StepKind.Entity or StepKind.Id or StepKind.Number or StepKind.String or StepKind.Symbol);
        return Tokens[value.Index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AsNumber(StepValue value)
    {
        Debug.Assert(value.Kind == StepKind.Number);
        return AsToken(value).AsNumber();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt128 AsId(StepValue value)
    {
        Debug.Assert(value.Kind == StepKind.Id);
        return AsToken(value).ToUInt128();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string AsString(StepValue value)
    {
        return Encoding.ASCII.GetString(AsToken(value).Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string AsTrimmedString(StepValue value)
    {
        Debug.Assert(value.Kind is StepKind.String or StepKind.Symbol);
        return Encoding.ASCII.GetString(AsToken(value).Span[1..^1]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepValue[] AsArray(StepValue value)
    {
        Debug.Assert(value.Kind == StepKind.List);
        var n = value.Count;
        var offset = value.Index;
        var r = new StepValue[n];
        for (var i = 0; i < n; ++i)
            r[i] = Values[i + offset];
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] AsNumbers(StepValue value)
    {
        Debug.Assert(value.Kind == StepKind.List);
        var vals = AsArray(value);
        var r = new double[vals.Length];
        for (var i = 0; i < vals.Length; ++i)
            r[i] = AsNumber(vals[i]);
        return r;
    }

    public UInt128[] AsIds(StepValue value)
    {
        Debug.Assert(value.Kind == StepKind.List);
        var vals = AsArray(value);
        var r = new UInt128[vals.Length];
        for (var i = 0; i < vals.Length; ++i)
            r[i] = AsId(vals[i]);
        return r;
    }
}