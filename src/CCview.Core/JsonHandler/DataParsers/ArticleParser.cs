using CCview.Core.DataClasses;
using CCview.Core.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCview.Core.Services;

namespace CCview.Core.JsonHandler.DataParsers
{
    public static class ArticleParser
    {
        public static IReadOnlyDictionary<int, Article> LoadArticles(string filePath)
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
            Dictionary<int, Article> articles = new(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                JArray articleArray = JsonUtils.ExpectArray(array[i], filePath, $"$[{i}]");
                JsonUtils.ExpectArrayLengthAtLeast(articleArray, 4, filePath, $"$[{i}]");

                int id = JsonUtils.GetIntAt(articleArray, 0, filePath, $"$[{i}]");
                int date = JsonUtils.GetIntAt(articleArray, 1, filePath, $"$[{i}]");
                string title = JsonUtils.GetStringAt(articleArray, 2, filePath, $"$[{i}]");
                string citation = JsonUtils.GetStringAt(articleArray, 3, filePath, $"[{i}]", true);

                if (articles.ContainsKey(id))
                {
                    throw new JsonValidationException($"Duplicate article id {id} found", filePath, $"$[{i}][0]");
                }

                articles[id] = new Article(id, date, title, citation);
            }
            return articles;
        }
        public static JArray ToJArray<T>(T article) where T : IArticle
        {
            return new(
                JToken.FromObject(article.GetId()),
                JToken.FromObject(article.GetBirthday()),
                JToken.FromObject(article.GetName()),
                JToken.FromObject(article.GetCitation())
            );
        }

        public static JArray ArticlesToJArray<T>(IReadOnlyDictionary<int, T> articles) where T : IArticle
        {
            return JArray.FromObject(articles.Values.Select(article => ToJArray(article)));
            //JArray root = [];
            //foreach (IArticle article in articles.Values)
            //{
            //    root.Add(ToJArray(article));
            //}
            //return root;
        }
        public static Article Deserialise(string json)
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
                Logging.LogDebug("Datetime stamp of this json is malformed.");
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
}
