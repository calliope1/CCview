using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Interfaces;

namespace CCview.Core.DataClasses
{
    public class Model : IModel
    {
        public int Id { get; private set; } = -1;
        public int GetId() { return Id; }
        public int ArticleId { get; private set; } = -1;
        public int GetArticleId() { return ArticleId; }
        public string Description { get; set; } = "No description provided.";
        public string GetDescription() { return Description; }
        public HashSet<ModelValue> Values { get; set; } = [];
        public HashSet<ModelValue> GetValIds() { return Values; }

        private static readonly int Cid = RelationType.IndexFromChar('C');
        public Model(int id, Article article, string description, HashSet<ModelValue> values)
        {
            Id = id;
            Description = description;
            ArticleId = article.Id;
            Values = values;
        }
        public Model(int id, int articleId, string description, List<List<int>> values)
        {
            Id = id;
            ArticleId = articleId;
            Description = description;
            Values = [.. values.Select(value => new ModelValue(value))];
        }
        public Model() { }
        public void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            ArticleId = args[1].Value<int>();
            Description = args[3].Value<string>() ?? "No description provided!";
            foreach (JArray alephArray in args[4].Cast<JArray>())
            {
                List<int> newList = alephArray.Value<List<int>>()!;
                Values.Add(new(newList));
            }
        }
        public override bool Equals(object? obj)
        {
            return obj is Model other && Id.Equals(other.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Model ID:{Id} '{Description}' from article ID{ArticleId}";
        }
    }
}