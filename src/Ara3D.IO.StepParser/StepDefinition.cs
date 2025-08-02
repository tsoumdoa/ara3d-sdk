using System;
using System.Runtime.CompilerServices;

namespace Ara3D.IO.StepParser
{
    public readonly struct StepDefinition
    {
        public readonly StepToken IdToken;
        public readonly int ValueIndex;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StepDefinition(StepToken idToken, int valueIndex)
        {
            IdToken = idToken;
            ValueIndex = valueIndex;
        }

        public UInt128 Id
            => IdToken.ToUInt128();
    }
}