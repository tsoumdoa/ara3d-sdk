using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ara3D.IO.StepParser;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EncodeIndexAndCount(uint index, uint count)
    {
        // Count must be under 2^28, since we store it in the lower 28 bits of a long.
        Debug.Assert(count < (uint.MaxValue >> 4));
        return ((ulong)index << 32) | (count << 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecodeIndexAndCount(ulong encoded, out uint index, out uint count)
    {
        index = (uint)(encoded >> 32);
        count = (uint)((encoded & 0xFFFFFFFF) >> 4);
    }
}