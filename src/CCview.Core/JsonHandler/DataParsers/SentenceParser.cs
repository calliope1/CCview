using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCview.Core.DataClasses;
using CCview.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace CCview.Core.JsonHandler.DataParsers
{
    public static class SentenceParser
    {
        public static Sentence Parse(JArray sentenceArray, string filePath, string path)
        {
            JsonUtils.ExpectArrayLengthAtLeast(sentenceArray, 2, filePath, path);
            int relationIndex = JsonUtils.GetIntAt(sentenceArray, 0, filePath, path);
            JArray idsArray = JsonUtils.ExpectArray(sentenceArray[1], filePath, $"{path}[1]");
            JsonUtils.ExpectArrayLengthAtLeast(idsArray, 2, filePath, $"{path}[1]");
            List<int> ids = new(idsArray.Count);
            for (int i = 0; i < idsArray.Count; i++)
            {
                ids.Add(JsonUtils.GetIntAt(idsArray, i, filePath, $"{path}[1][{i}]"));
            }
            return new Sentence(relationIndex, ids);
        }

        public static JArray ToJArray(ISentence sentence)
        {
            JArray jArray = [];
            int typeId = sentence.GetRelationType().GetIndex();
            jArray.Add(typeId);
            IEnumerable<int> ids = sentence.GetIds();
            JArray idsArray = [];
            foreach (int id in ids)
            {
                idsArray.Add(id);
            }
            jArray.Add(idsArray);
            return jArray;
        }
    }
}
