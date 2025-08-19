using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Services;
using CCview.Core.Interfaces;

namespace CCview.Core.DataClasses
{
    public class Article : IArticle
    {
        public int Id { get; private set; } = -1;
        // Generally represented as YYYYMMDD, with XX = 99 if not found. MaxValue for 'no year' for simplicity
        public int Date { get; private set; } = int.MaxValue;
        public int GetBirthday() => Date;
        public string Name { get; private set; } = "Article name required!";
        public string GetName() => Name;
        public string Citation { get; private set; } = "Citation required!";
        public string GetCitation() => Citation;
        public int GetId() => Id;
        // We're not going to save the subordinate Theorems, since these can be reconstructed at runtime
        protected List<string> FieldsToSave => ["Id", "Date", "Name", "Citation"];
        public Article(int id, int date, string name, string citation)
        {
            Id = id;
            Date = date;
            Name = name;
            Citation = citation;
        }
        public Article() { }
        public void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            Date = args[1].Value<int>();
            Name = args[2].Value<string>() ?? "Article name required!";
            Citation = args[3].Value<string>() ?? "Citation required!";
        }
        public override bool Equals(object? obj)
        {
            return obj is Article other && other.Id == Id;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Article {Name} (ID: {Id}) from {Date}";
        }
        public void GetNewId(RelationDatabase rD, bool fast = false)
        {
            Id = RelationDatabase.NewDictId(rD.Articles, fast);
        }
    }
}
