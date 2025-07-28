using CCView.CardinalData;
using CCView.CardinalData.Compute;
using JsonHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using CC = CCView.CardinalData.CardinalCharacteristic;
//using Microsoft.Extensions.ObjectPool;

namespace JsonHandler
{
    public class JsonInterface
    {
        public static string GetAssetsPath(string filename)
        {
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"../../../"));
            return Path.Combine(projectRoot, "assets", filename);
        }
        public static List<CC> LoadCardinals(string path)
        {
            using (StreamReader r = new(path))
            {
                string json = r.ReadToEnd();
                List<CC>? items = JsonConvert.DeserializeObject<List<CC>>(json); // The ? here tells me that it could be null, and that's ok
                return items ?? [];
            }
        }
        public static string SaveList<T>(String path, List<T> list)
        {
            string json = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(path, json);
            return json;
        }
        // We could probably get away with Func<T, object> below, but we're keeping the onus on the caller to decide what the actual string should be
        public static string SaveListParams<T>(String path, List<T> list, params Func<T, JsonSaveElement>[] selectors)
        {
            string json = JsonConvert.SerializeObject(list.Select(item =>
            {
                JArray thisObject = [];
                foreach (var selector in selectors)
                {
                    var val = selector(item);
                    thisObject.Add(val.Value);
                }
                return thisObject;
            }).ToArray(), Formatting.Indented);
            File.WriteAllText(path, json);
            return json;
        }

        public static string SaveRelations(string path, IEnumerable<Relation> relations)
        {
            return SaveListParams<Relation>(path, relations.ToList() ?? [],
                r => (r.Item1.Id),
                r => (r.Item2.Id),
                r => (RelationTypeToInt(r.Type)),
                r => (r.ArticleId)
                );
        }
        public static HashSet<Relation> LoadRelations(string path, List<CC> cardinals) // Only loads the relations for the listed cardinals
        {
            // Add catches for if the entries don't have an id in the list of cardinals
            var byId = cardinals.ToDictionary(c => c.Id);
            using (StreamReader r = new(path))
            {
                string json = r.ReadToEnd();
                var relationTuples = JsonConvert.DeserializeObject<List<int[]>>(json) ?? [];
                var result = new HashSet<Relation>();
                foreach (var tup in relationTuples)
                {
                    //if (tup.Length != 5 || !byId.ContainsKey(tup[0]) || !byId.ContainsKey(tup[1])) // This one is better once we know how large a relation is
                    if (!byId.ContainsKey(tup[0]) || !byId.ContainsKey(tup[1]))
                    {
                        continue; // Optionally log
                    }
                    result.Add(new Relation(byId[tup[0]], byId[tup[1]], IntToRelationType(tup[2])));
                }
                return result;
            }
        }

        private static int RelationTypeToInt(char type)
        {
            return Relation.TypeIndices.First(t => t == type);
        }
        private static char IntToRelationType(int type)
        {
            if (type >= Relation.TypeIndices.Count)
            {
                throw new ArgumentException($"{type} is not a valid relation type id.");
            }
            return Relation.TypeIndices[type];
        }
        public static string JsonSaveByProperties(String path, IEnumerable<JsonSaveable> objs)
        {
            JArray outFile = [];
            foreach (JsonSaveable obj in objs)
            {
                outFile.Add(obj.TurnToJson());
            }
            File.WriteAllText(path, outFile.ToString(Formatting.Indented));
            return outFile.ToString(Formatting.Indented);
        }
    }
    // For safe type checking when saving
    // Currently of the form "String or Int", but it can be expanded as needed
    public class JsonSaveElement
    {
        public object Value { get; }
        private JsonSaveElement(object value)
        {
            if (value is not string && value is not int)
            {
                throw new ArgumentException($"Object {value} is not string or int.");
            }
            Value = value;
        }
        public static implicit operator JsonSaveElement(string s) => new JsonSaveElement(s);
        public static implicit operator JsonSaveElement(int i) => new JsonSaveElement(i);
    }
    public abstract class JsonSaveable
    {
        protected abstract List<string> FieldsToSave { get; }
        //protected abstract List<Func<object, object[], object>> Decompress { get; }
        public JArray TurnToJson()
        {
            JArray jsonArray = [];
            Type type = this.GetType();
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
                if (currentObject == null)
                {
                    continue;
                }
                PropertyInfo finalProp = currentType.GetProperty(finalPropName)!;
                if (finalProp == null)
                {
                    continue;
                }
                object value = finalProp.GetValue(currentObject)!;
                jsonArray.Add(JToken.FromObject(value));
            }
            return jsonArray;
        }
        public List<JToken> LoadFromJson(string json)
        {
            //Type type = this.GetType();
            var jsonList = JArray.Parse(json).ToList();
            if (jsonList.Count != FieldsToSave.Count)
            {
                throw new InvalidOperationException("JSON array element does not match FieldsToSave count.");
            }
            return jsonList;
        }
        // From the old 'clever' loading system:
        //for (int i = 0; i < FieldsToSave.Count; i++)
        //{
        //    string path = FieldsToSave[i];
        //    object value = jsonArray[i].ToString();
        //    string[] parts = path.Split('.');
        //    string parentPropName = parts[0];
        //    PropertyInfo prop = type.GetProperty(parentPropName)!;
        //    prop.SetValue(this, Decompress[i](value, args));
        //}
        public static string Save<T>(string path, IEnumerable<T> list) where T : JsonSaveable
        {
            string json = JsonConvert.SerializeObject(list.Select(item => item.TurnToJson()));
            File.WriteAllText(path, json);
            return json;
        }
    }
}