using System;
using System.Runtime.CompilerServices;
using Ara3D.Memory;

namespace Ara3D.IO.StepParser;

public readonly unsafe struct StepToken : IEquatable<StepToken>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AsId()
        => int.Parse(Span.Slice(1));

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

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(End - Begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(StepToken other)
        => Length == other.Length && MemEquals(Begin, other.Begin, Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool MemEquals(byte* a, byte* b, int len)
    {
        // Compare 8, then 4, then tail bytes (unaligned OK)
        int i = 0;
        for (; i + 8 <= len; i += 8)
            if (Unsafe.ReadUnaligned<ulong>(a + i) != Unsafe.ReadUnaligned<ulong>(b + i)) return false;
        if ((len - i) >= 4)
        {
            if (Unsafe.ReadUnaligned<uint>(a + i) != Unsafe.ReadUnaligned<uint>(b + i)) return false;
            i += 4;
        }
        for (; i < len; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj is StepToken other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.AddBytes(Span); 
        return hc.ToHashCode();
    }

    public static bool operator ==(StepToken left, StepToken right) => left.Equals(right);
    public static bool operator !=(StepToken left, StepToken right) => !left.Equals(right);
}