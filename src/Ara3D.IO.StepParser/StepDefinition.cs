using System;
using System.Runtime.CompilerServices;

namespace Ara3D.IO.StepParser
{
    public readonly unsafe struct StepDefinition
    {
        public readonly StepToken IdToken;
        public readonly int ValueIndex;
        public readonly byte* End;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StepDefinition(StepToken idToken, int valueIndex, byte* end)
        {
            IdToken = idToken;
            ValueIndex = valueIndex;
            End = end;
        }

        public int Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IdToken.AsId();
        }

        public byte* Begin
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IdToken.Begin;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsSpan() 
            => new(Begin, (int)(End - Begin));
    }
}