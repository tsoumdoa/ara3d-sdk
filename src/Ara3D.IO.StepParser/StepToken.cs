using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Ara3D.Memory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ara3D.IO.StepParser;

public readonly unsafe struct StepToken
{
    public readonly byte* Begin;
    public readonly byte* End;

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

    public ReadOnlySpan<byte> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Begin, (int)(End - Begin));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AsNumber()
        => double.Parse(Span);

    /// <summary>
    /// Reads 16 consecutive bytes from <paramref name="p"/> into a <see cref="UInt128"/>
    /// and zeroes every byte from index <paramref name="n"/> (inclusive) up to byte 15.
    /// <para>Thus <c>n==0</c> ⇒ return 0, <c>n==16</c> ⇒ keep all 16 bytes.</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt128 ReadU128(byte* p, int n)
    {
        var value = Unsafe.ReadUnaligned<UInt128>(p);
        var mask = n == 16
            ? UInt128.MaxValue                   // avoid shift by 128
            : ((UInt128)1 << (n * 8)) - 1;
        return value & mask;
    }

    /// <summary>
    /// Read 16 bytes from the token, zeroing the tail bytes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt128 ToUInt128()
        => ReadU128(Begin, (int)(End - Begin));

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(End - Begin);
    }

    public bool Equals(StepToken other)
        => Span.SequenceEqual(other.Span);

    public override bool Equals(object? obj)
        => obj is StepToken other && Equals(other);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.AddBytes(Span); 
        return hc.ToHashCode();
    }

    public static bool operator ==(StepToken left, StepToken right) => left.Equals(right);
    public static bool operator !=(StepToken left, StepToken right) => !left.Equals(right);
}