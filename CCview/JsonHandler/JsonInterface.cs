using CCView.CardinalData;
using CCView.CardinalData.Compute;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.CommandLine;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using CC = CCView.CardinalData.CardinalCharacteristic;
//using Microsoft.Extensions.ObjectPool;

namespace CCView.JsonHandler
{
    public class JsonFileHandler
    {
        public static string Save<T>(string path, IEnumerable<JsonCRTP<T>> list) where T : JsonCRTP<T>
        {
            string json = JsonConvert.SerializeObject(list.Select(item => item.TurnToJson()), Formatting.Indented);
            File.WriteAllText(path, json);
            return json;
        }
        public static string Save<T>(string path, Dictionary<int, T> dict) where T : JsonCRTP<T> => Save<T>(path, dict.Values);
        public static List<JArray> LoadJsonData(string path)
        {
            using StreamReader r = new(path);
            string json = r.ReadToEnd();
            JArray jArray = JArray.Parse(json);
            List<JArray> listJArray = [];
            foreach (var item in jArray)
            {
                if (item is JArray innerArray)
                {
                    listJArray.Add(innerArray);
                }
                else
                {
                    throw new ArgumentException($"Non-array data in {path}.");
                }
            }
            return listJArray;
        }
        public static Dictionary<int, T> Load<T>(string path) where T : JsonCRTP<T>
        {
            List<JArray> data = LoadJsonData(path);
            Dictionary<int, T> values = [];
            foreach (JArray item in data)
            {
                var instance = Activator.CreateInstance<T>();
                instance.InstantiateFromJArray(item);
                values[instance.Id] = instance;
            }
            return values;
        }
        public static Dictionary<int, Relation> LoadRelations(string path, Dictionary<int, Theorem> theorems)
        {
            List<JArray> data = LoadJsonData(path);
            Dictionary<int, Relation> relations = [];
            foreach (JArray item in data)
            {
                Relation newRel = new();
                newRel.InstantiateFromJArray(item);
                foreach (AtomicRelation atom in newRel.Derivation)
                {
                    atom.Witness = theorems[atom.WitnessId];
                }
                relations[newRel.Id] = newRel;
            }
            return relations;
        }
        public static Dictionary<int, Model> LoadModels(string path, Dictionary<int, CC> cardinals, Dictionary<int, Article> articles, Dictionary<int, Theorem> theorems)
        {
            List<JArray> data = LoadJsonData(path);
            Dictionary<int, Model> models = [];
            foreach (JArray item in data)
            {
                Model instance = new();
                instance.InstantiateFromJArray(item);
                foreach (var val in instance.ValIds)
                {
                    (CC Cardinal, int Aleph, Theorem Witness) newVal = new(cardinals[val.ItemId], val.Aleph, theorems[val.ThmId]);
                    instance.Values.Add(newVal);
                }
                instance.Article = articles[instance.ArtId];
                models[instance.Id] = instance;
            }
            return models;
        }
        public static Dictionary<int, Theorem> LoadTheorems(string path, Dictionary<int, CC> cardinals, Dictionary<int, Article> articles)
        {
            Dictionary<int, Theorem> theorems = Load<Theorem>(path);
            foreach (Theorem thm in theorems.Values)
            {
                thm.Article = articles[thm.ArtId];
            }
            return theorems;
        }
        public static Article DeserializeArticle(string json)
        {
            JObject jObj = JObject.Parse(json);
            string dateStamp = (string?)jObj["result"]?["datestamp"] ?? "99999999";
            string title = (string?)jObj["result"]?["title"]?["title"] ?? "No title found";
            int dateCombined = 0;
            if (DateTime.TryParse(dateStamp, out DateTime date))
            {
                dateCombined = date.Year * 10000 + date.Month * 100 + date.Day;

            }
            else
            {
                Program.LoadLog("Datetime stamp of this json is malformed.");
                dateCombined = 99999999;
            }
            string citation = ToBibTeX(jObj);
            return new(-1, dateCombined, title, citation);
        }
        public static string ToBibTeX(JObject json)
        {
            if (json["result"] is not JObject result)
                return "// Invalid JSON: 'result' field missing";

            string id = result["id"]?.ToString()!;
            string title = result["title"]?["title"]?.ToString()!;
            string year = result["year"]?.ToString()!;

            var authors = result["contributors"]?["authors"] as JArray;
            string authorField = authors != null
                ? string.Join(" and ", authors.Select(a => a["name"]?.ToString()).Where(n => !string.IsNullOrWhiteSpace(n)))
                : null!;

            var series = result["source"]?["series"]?.FirstOrDefault();
            string journal = series?["short_title"]?.ToString()!;
            string volume = series?["volume"]?.ToString()!;
            string pages = series?["pages"]?.ToString()!;

            string citationKey = $"Zbl{id}";

            var sb = new StringBuilder();
            sb.AppendLine($"@article{{{citationKey},");

            void AddField(string name, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    sb.AppendLine($"  {name,-9}= \"{value}\",");
            }

            AddField("author", authorField);
            AddField("title", title);
            AddField("journal", journal);
            AddField("volume", volume);
            AddField("pages", pages);
            AddField("year", year);
            AddField("note", $"Zbl {id}");

            // Remove trailing comma from last field
            var bibtex = sb.ToString().TrimEnd(',', '\r', '\n') + "\n}";
            return bibtex;
        }
    }

    // Curiously Recurring Template Pattern
    // Reminds me of the Y combinator
    public abstract class JsonCRTP<T> where T : JsonCRTP<T>
    {
        protected abstract List<string> FieldsToSave { get; }
        public int Id { get; set; } = -1;
        public abstract void InstantiateFromJArray(JArray jsonData);
        private static JArray IteratedTurnToToken(IEnumerable enumerable)
        {
            JArray newArray = [];
            foreach (object item in enumerable)
            {
                if (item is IntThree intThree)
                {
                    newArray.Add(JArray.FromObject(intThree.ToList()));
                }
                else if (item is JsonToArray jta)
                {
                    newArray.Add(jta.TurnToJson());
                }
                else if (item is IEnumerable subenumerable && item is not string)
                {
                    JArray subArray = IteratedTurnToToken(subenumerable);
                    newArray.Add(subArray);
                }
                else
                {
                    newArray.Add(JToken.FromObject(item));
                }
            }
            return newArray;
        }
        public virtual JArray TurnToJson()
        {
            JArray jsonArray = [];
            Type type = typeof(T);
            foreach (string path in FieldsToSave)
            {
                object currentObject = this;
                Type currentType = type;
                string[] parts = path.Split('.');
                string finalPropName = parts[^1];
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    PropertyInfo prop = currentType.GetProperty(parts[i])!;
                    if (prop == null)
                    {
                        currentObject = null!;
                        break;
                    }
                    currentObject = prop.GetValue(currentObject)!;
                    if (currentObject == null)
                    {
                        break;
                    }
                    currentType = currentObject.GetType();
                }
                if (currentObject == null) continue;
                PropertyInfo finalProp = currentType.GetProperty(finalPropName)!;
                if (finalProp == null) continue;
                object value = finalProp.GetValue(currentObject)!;
                Type T = value.GetType();
                if (value is JsonToArray jta)
                {
                    jsonArray.Add(jta.TurnToJson());
                }
                else if (value is IntThree intThree)
                {
                    jsonArray.Add(JArray.FromObject(intThree.ToList()));
                }
                else if (value is IEnumerable enumerable && value is not string)
                {
                    JArray subArray = IteratedTurnToToken(enumerable);
                    jsonArray.Add(subArray);
                }
                else
                {
                    jsonArray.Add(JToken.FromObject(value));
                }
            }
            return jsonArray;
        }
        public List<JToken> LoadFromJson(string json)
        {
            var jsonList = JArray.Parse(json).ToList();
            if (jsonList.Count != FieldsToSave.Count)
            {
                throw new InvalidOperationException("JSON array element does not match FieldsToSave count.");
            }
            return jsonList;
        }
    }
    // This is for objects that want to be turned to JArrays but aren't directly being saved, such as Sentences
    public abstract class JsonToArray
    {
        public abstract JArray TurnToJson();
    }
    public readonly struct IntThree : IEquatable<IntThree>
    {
        public readonly int ItemId, Aleph, ThmId;
        public IntThree(int a, int b, int c)
        {
            ItemId = a;
            Aleph = b;
            ThmId = c;
        }
        public IntThree(int[] array)
        {
            if (array == null || array.Length != 3)
            {
                throw new ArgumentException("Array must be of length 3.");
            }
            ItemId = array[0];
            Aleph = array[1];
            ThmId = array[2];
        }
        public IntThree(List<int> list)
        {
            if (list == null || list.Count != 3)
            {
                throw new ArgumentException("List must be of length 3.");
            }
            ItemId = list[0];
            Aleph = list[1];
            ThmId = list[2];
        }
        public bool Equals(IntThree other)
        {
            return ItemId == other.ItemId
                && Aleph == other.Aleph
                && ThmId == other.ThmId;
        }
        public override bool Equals(object? obj)
        {
            return obj is IntThree other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, Aleph, ThmId);
        }
        public int[] ToArray() => [ItemId, Aleph, ThmId];
        public List<int> ToList() => [ItemId, Aleph, ThmId];
        public HashSet<int> ToHashSet() => [ItemId, Aleph, ThmId];
        public static bool operator ==(IntThree left, IntThree right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(IntThree left, IntThree right)
        {
            return !(left == right);
        }
        public static implicit operator IntThree(List<int> list) => new(list);
        public static implicit operator IntThree(int[] array) => new(array);
    }
}