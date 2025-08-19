using CCview.Core.DataClasses;
using CCview.Core.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.Core.JsonHandler.DataParsers
{
    public static class RelationParser
    {
        public static IReadOnlyDictionary<int, Relation> LoadRelations(string filePath, IReadOnlyDictionary<int, Theorem> theorems)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"JSON asset not found: {filePath}", filePath);
            }
            JToken root;
            string txt = File.ReadAllText(filePath);
            try
            {
                root = JToken.Parse(txt);
            }
            catch (Newtonsoft.Json.JsonReaderException exception)
            {
                throw new JsonValidationException($"Invalid JSON syntax: {exception.Message}", filePath, $"Line {exception.LineNumber}, position {exception.LinePosition}");
            }
            JArray array = JsonUtils.ExpectArray(root, filePath, "$");
            Dictionary<int, Relation> relations = new(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                JArray relationArray = JsonUtils.ExpectArray(array[i], filePath, "$");
                JsonUtils.ExpectArrayLengthAtLeast(relationArray, 3, filePath, $"$[{i}]");

                int id = JsonUtils.GetIntAt(relationArray, 0, filePath, $"$[{i}]");

                if (relations.ContainsKey(id))
                {
                    throw new JsonValidationException($"Duplicate article id {id} found", filePath, $"$[{i}][0]");
                }

                JArray statementArray = JsonUtils.ExpectArray(relationArray[1], filePath, $"$[{i}]");
                Sentence statement = SentenceParser.Parse(statementArray, filePath, $"$[{i}][1]");
                JArray derivationArray = JsonUtils.ExpectArray(relationArray[2], filePath, $"[{i}]");
                HashSet<AtomicRelation> derivation = [];
                for (int j = 0; j < derivationArray.Count; j++)
                {
                    JArray atomicRelationArray = JsonUtils.ExpectArray(derivationArray[j], filePath, $"[{i}]");
                    AtomicRelation newAtom = AtomicRelationParser.Parse(atomicRelationArray, theorems, filePath, $"[{i}][{j}]");
                    derivation.Add(newAtom);
                }
                relations[id] = new(id, statement, derivation);
            }
            return relations;
        }
        public static JArray ToJArray<T>(T relation) where T : IRelation<T>
        {
            JArray derivationArray = [];
            foreach (IAtomicRelation atomicRelation in relation.GetDerivation())
            {
                derivationArray.Add(AtomicRelationParser.ToJArray(atomicRelation));
            }
            return new JArray(
                JToken.FromObject(relation.GetId()),
                SentenceParser.ToJArray(relation.GetStatement()),
                derivationArray
            );
        }

        public static JArray RelationsToJArray<T>(IReadOnlyDictionary<int, T> relations) where T : IRelation<T>
        {
            JArray root = [];
            foreach (T relation in relations.Values)
            {
                root.Add(ToJArray(relation));
            }
            return root;
        }
    }
}
