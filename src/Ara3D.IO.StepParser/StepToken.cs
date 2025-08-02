using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Ara3D.Memory;

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
        Debug.Assert((uint)n <= 16);

        // 1. Unaligned 16‑byte load
        var value = Unsafe.ReadUnaligned<UInt128>(p);

        // 2. Build a 128‑bit mask that has the low n bytes = 0xFF, rest = 0
        //    mask = (1u128 << (n*8)) - 1u128
        var mask = n == 16
            ? UInt128.MaxValue                   // avoid shift by 128
            : ((UInt128)1 << (n * 8)) - 1;

        // 3. Clear the tail bytes
        var result = value & mask;

        // mask = 0…0011…11
        Debug.Assert((mask & (mask + 1)) == 0u, "mask should be contiguous ones");

        // Cleared region really is zero
        Debug.Assert((result & ~mask) == 0u, "high bytes after n must be zero");

        // Kept region matches original value
        Debug.Assert(((result ^ value) & mask) == 0u, "kept bytes must equal source");
        
        return result;
    }

    /// <summary>
    /// Read 16 bytes from the token, zeroing the tail bytes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt128 ToUInt128()
        => ReadU128(Begin, (int)(End - Begin));
}