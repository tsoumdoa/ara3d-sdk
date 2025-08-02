using System;
using System.Collections.Generic;
using Ara3D.Utils;

namespace Ara3D.IO.StepParser
{
    public class StepGraph
    {
        public StepValueData Data;
        public Dictionary<UInt128, StepDefinition> Definitions = new();
        public MultiDictionary<UInt128, UInt128> Relations = new();
        public MultiDictionary<UInt128, UInt128> InverseRelations = new();
        public Dictionary<UInt128, StepValue[]> Attributes = new();

        public string GetEntityName(UInt128 id)
            => Data.GetEntityName(Definitions[id]);

        public StepGraph(StepDocument doc)
        {
            Data = doc.ValueData;
            foreach (var def in doc.Definitions)
            {
                var defId = def.Id;
                Definitions.Add(defId, def);
                var attrs = Data.GetAttributes(def);
                Attributes.Add(defId, attrs);
            }

            foreach (var kv in Attributes)
            {
                var defId = kv.Key;
                foreach (var val in kv.Value)
                {
                    if (val.IsId)
                    {
                        var valId = Data.AsId(val);
                        Relations.Add(defId, valId);
                        InverseRelations.Add(valId, defId);
                    }
                }
            }
        }
    }
}
