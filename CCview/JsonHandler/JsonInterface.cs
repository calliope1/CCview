using CCView.CardinalData;
using CCView.CardinalData.Compute;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
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
        public static List<T> Load<T>(string path) where T : JsonCRTP<T>
        {
            List<JArray> data = LoadJsonData(path);
            List<T> values = [];
            foreach (JArray item in data)
            {
                var instance = Activator.CreateInstance<T>();
                instance.InstantiateFromJArray(item);
                values.Add(instance);
            }
            return values;
        }
        public static List<Relation> LoadRelations(string path, List<CC> cardinals)
        {
            List<Relation> relations = Load<Relation>(path);
            List<int> idIndList = RelationDatabase.InitIndexList(cardinals, c => c.Id, -1);
            foreach (Relation r in relations)
            {
                r.Item1 = cardinals[idIndList[r.Item1Id]];
                r.Item2 = cardinals[idIndList[r.Item2Id]];
            }
            return relations;
        }
    }

    // Curiously Recurring Template Pattern
    // Reminds me of the Y combinator
    public abstract class JsonCRTP<T> where T : JsonCRTP<T>
    {
        protected abstract List<string> FieldsToSave { get; }
        public abstract void InstantiateFromJArray(JArray jsonData);
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
            var jsonList = JArray.Parse(json).ToList();
            if (jsonList.Count != FieldsToSave.Count)
            {
                throw new InvalidOperationException("JSON array element does not match FieldsToSave count.");
            }
            return jsonList;
        }
    }
}