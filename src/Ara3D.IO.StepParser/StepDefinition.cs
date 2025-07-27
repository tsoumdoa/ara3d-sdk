using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ara3D.IO.StepParser
{
    public readonly struct StepDefinition
    {
        public readonly ulong Id;
        public readonly int Index;
        public readonly StepValues Values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StepDefinition(ulong id, int index, StepValues values)
        {
            Id = id;
            Index = index;
            Values = values;
        }

        public StepValue GetEntityValue()
        {
            var r = Values.Values[Index];
            Debug.Assert(r.Kind == StepKind.Entity);
            return r;
        }

        public string GetEntityName()
            => Values.GetString(GetEntityValue());

        public StepValue GetAttributesValue()
        {
            var r = Values.Values[Index + 1];
            Debug.Assert(r.Kind == StepKind.List);
            return r;
        }

        public string GetAttributesString()
            => Values.GetString(GetAttributesValue());

        public StepValue[] GetAttributes()
            => GetAttributesValue().GetListValues(Values);

        public override string ToString()
            => $"{GetEntityName()}({GetAttributesString()})";
    }
}