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
    public static class TheoremParser
    {
        public static Theorem Parse(JArray jArray, string filePath, string path)
        {
            JsonUtils.ExpectArrayLengthAtLeast(jArray, 4, filePath, path);
            // id, articleid, description, results
            int id = JsonUtils.GetIntAt(jArray, 0, filePath, path);
            int articleId = JsonUtils.GetIntAt(jArray, 1, filePath, path);
            string description = JsonUtils.GetStringAt(jArray, 2, filePath, path, false);
            JArray resultsArray = JsonUtils.ExpectArray(jArray[3], filePath, $"{path}[3]");
            HashSet<Sentence> results = [];
            for (int i = 0; i < resultsArray.Count; i++)
            {
                JArray resultArray = JsonUtils.ExpectArray(resultsArray[i], filePath, $"{path}[3]");
                results.Add(SentenceParser.Parse(resultArray, filePath, $"{path}[3][{i}]"));
            }
            return new(id, articleId, description, results);
        }

        public static IReadOnlyDictionary<int, Theorem> LoadTheorems(string filePath, IReadOnlyDictionary<int, Article> articles)
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
            Dictionary<int, Theorem> theorems = new(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                JArray theoremArray = JsonUtils.ExpectArray(array[i], filePath, "$");
                Theorem theorem = Parse(theoremArray, filePath, $"$[{i}]");
                if (theorems.ContainsKey(theorem.Id))
                {
                    throw new JsonValidationException($"Duplicate theorem id {theorem.Id} found", filePath, $"$[{i}][0]");
                }
                theorem.SetArticleByDictionary(articles);
                theorems[theorem.Id] = theorem;
            }
            return theorems;
        }
        public static JArray ToJArray(ITheorem theorem)
        {
            JArray resultsArray = [];
            foreach (ISentence result in theorem.GetResults())
            {
                resultsArray.Add(SentenceParser.ToJArray(result));
            }
            return new(
                JToken.FromObject(theorem.Id),
                JToken.FromObject(theorem.ArtId),
                JToken.FromObject(theorem.Description),
                resultsArray
            );
        }
        public static JArray TheoremsToJArray<T>(IReadOnlyDictionary<int, T> theorems) where T : ITheorem
        {
            JArray array = [];
            foreach (ITheorem theorem in theorems.Values)
            {
                array.Add(ToJArray(theorem));
            }
            return array;
        }
    }
}
