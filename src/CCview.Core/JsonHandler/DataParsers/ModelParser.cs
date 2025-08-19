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
    public static class ModelParser
    {
        public static Model Parse(JArray modelArray, string filePath, string path)
        {
            JsonUtils.ExpectArrayLengthAtLeast(modelArray, 4, filePath, path);
            int id = JsonUtils.GetIntAt(modelArray, 0, filePath, path);
            int articleId = JsonUtils.GetIntAt(modelArray, 1, filePath, path);
            string description = JsonUtils.GetStringAt(modelArray, 2, filePath, path);
            JArray valuesArray = JsonUtils.ExpectArray(modelArray[3], filePath, "[0][3]");
            List<List<int>> values = [];
            for (int j = 0; j < valuesArray.Count; j++)
            {
                JArray value = JsonUtils.ExpectArray(valuesArray[j], filePath, $"[0][3][{j}]");
                JsonUtils.ExpectArrayLengthAtLeast(value, 3, filePath, $"[0][3][{j}]");
                List<int> newValue = [];
                for (int k = 0; k < value.Count; k++)
                {
                    newValue.Add(JsonUtils.GetIntAt(value, k, filePath, $"[0][3][{j}]"));
                }
                values.Add(newValue);
            }
            return new Model(id, articleId, description, values);
        }
        public static IReadOnlyDictionary<int, Model> LoadModels(string filePath)
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
            Dictionary<int, Model> models = new(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                JArray modelArray = JsonUtils.ExpectArray(array[i], filePath, $"$[{i}]");
                Model newModel = Parse(modelArray, filePath, $"$[{i}]");
                if (models.ContainsKey(newModel.Id))
                {
                    throw new JsonValidationException($"Duplicate model ID {newModel.Id} found in file: {filePath}", filePath, $"$[{i}]");
                }
                models[newModel.Id] = newModel;
            }
            return models;
        }
        public static JArray ToJArray(IModel model)
        {
            HashSet<ModelValue> values = model.GetValIds();
            JArray valuesArray = [];
            foreach (ModelValue value in values)
            {
                valuesArray.Add(
                    new JArray(
                        JToken.FromObject(value.ItemId),
                        JToken.FromObject(value.Aleph),
                        JToken.FromObject(value.ThmId)
                        )
                    );
            }
            return new JArray(
                JToken.FromObject(model.GetId()),
                JToken.FromObject(model.GetArticleId()),
                JToken.FromObject(model.GetDescription()),
                valuesArray
                );
        }
        public static JArray ModelsToJArray<T>(Dictionary<int, T> models) where T : IModel
        {
            JArray root = [];
            foreach (IModel model in models.Values)
            {
                root.Add(ToJArray(model));
            }
            return root;
        }
    }
}
