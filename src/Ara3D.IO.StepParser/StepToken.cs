using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Ara3D.Memory;

namespace Ara3D.IO.StepParser;

public unsafe struct StepToken
{
    public byte* Begin;
    public byte* End;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StepToken(byte* begin, byte* end)
    {
        Begin = begin;
        End = end;
    }

    public StepTokenType Type
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StepTokenizer.TokenLookup[*Begin];
    }

    public ByteSlice Slice 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new (Begin, End);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
        => Slice.ToAsciiString();
}