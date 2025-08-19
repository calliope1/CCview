using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CCview.Core.DataClasses;
using CCview.Core.Interfaces;

namespace CCview.Core.JsonHandler.DataParsers
{
    public static class AtomicRelationParser
    {
        public static AtomicRelation Parse(JArray atomicRelationArray, IReadOnlyDictionary<int, Theorem> theorems, string filePath, string path)
        {
            JsonUtils.ExpectArrayLengthAtLeast(atomicRelationArray, 2, filePath, path);
            int witnessId = JsonUtils.GetIntAt(atomicRelationArray, 0, filePath, path);
            JArray sentenceArray = JsonUtils.ExpectArray(atomicRelationArray[1], filePath, path);
            Sentence statement = SentenceParser.Parse(sentenceArray, filePath, $"{path}[1]");
            return new(statement, theorems[witnessId]);
        }

        public static JArray ToJArray(IAtomicRelation atomicRelation)
        {
            return new(
                JToken.FromObject(atomicRelation.GetWitnessId()),
                SentenceParser.ToJArray(atomicRelation.GetStatement())
                );
        }
    }
}
