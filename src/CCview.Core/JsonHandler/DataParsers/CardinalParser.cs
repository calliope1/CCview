using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using Newtonsoft.Json.Linq;
using CCview.Core.Interfaces;

namespace CCview.Core.JsonHandler.DataParsers
{
    public static class CardinalParser
    {
        public static IReadOnlyDictionary<int, CC> LoadCardinals(string filePath)
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
            Dictionary<int, CC> cardinals = new(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                JArray cardinalArray = JsonUtils.ExpectArray(array[i], filePath, $"$[{i}]");
                JsonUtils.ExpectArrayLengthAtLeast(cardinalArray, 3, filePath, $"$[{i}]");

                int id = JsonUtils.GetIntAt(cardinalArray, 0, filePath, $"$[{i}]");
                string name = JsonUtils.GetStringAt(cardinalArray, 1, filePath, $"$[{i}]");
                string shortName = JsonUtils.GetStringAt(cardinalArray, 2, filePath, $"$[{i}]");

                if (id < 0)
                {
                    throw new JsonValidationException($"Cardinal id must be non-negative, found {id}", filePath, $"$[{i}][0]");
                }

                if (cardinals.ContainsKey(id))
                {
                    throw new JsonValidationException($"Duplicate cardinal id {id} found", filePath, $"$[{i}][0]");
                }

                cardinals[id] = new CC(id, name, shortName);
            }
            return cardinals;
        }

        public static JArray CardinalsToJArray<T>(IReadOnlyDictionary<int, T> cardinals) where T : ICardinalCharacteristic
        {
            JArray root = [];
            foreach (ICardinalCharacteristic cardinal in cardinals.Values)
            {
                root.Add(
                    new JArray(
                        JToken.FromObject(cardinal.GetId()),
                        JToken.FromObject(cardinal.GetName()),
                        JToken.FromObject(cardinal.GetEquationSymbol())
                        )
                );
            }
            return root;
        }
    }
}
