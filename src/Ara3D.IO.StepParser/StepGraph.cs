using System;
using System.Collections.Generic;
using Ara3D.Utils;

namespace Ara3D.IO.StepParser
{
    public class StepGraph
    {
        public StepRawValueData Data;
        public Dictionary<int, StepDefinition> Definitions = new();
        public MultiDictionary<int, int> Relations = new();
        public MultiDictionary<int, int> InverseRelations = new();
        public Dictionary<int, StepRawValue[]> Attributes = new();

        public string GetEntityName(int id)
            => Data.GetEntityName(Definitions[id]);

        public StepGraph(StepDocument doc)
        {
            Data = doc.RawValueData;
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
