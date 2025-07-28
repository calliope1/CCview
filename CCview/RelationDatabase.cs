using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CCView.GraphLogic.Algorithms;
using CC = CCView.CardinalData.CardinalCharacteristic;
using QuikGraph;
using JsonHandler;
using Newtonsoft.Json.Linq;
//using Microsoft.Extensions.ObjectPool;

namespace CCView.CardinalData
{
    public class CardinalCharacteristic(int id, string? name, string? symbol)
    {
        public int Id { get; private set; } = id;
        public string Name { get; set; } = name ?? "No name assigned";
        public string SymbolString { get; set; } = symbol ?? "X";
        private int ArtId { get; set; } = -1;

        [JsonConstructor] // Telling Json.NET to use this constructor

        public CardinalCharacteristic(int id, string name) : this(id, name, "X")
        {
        }

        public override bool Equals(object? obj)
        {
            return obj is CardinalCharacteristic other && this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id})";
        }
    }

    public class Article
    {
        public int Id { get; private set; } = -1;
        public int Year { get; private set; } = int.MaxValue;
        public string Name { get; private set; } = "Article name required!";
        public string Citation { get; private set; } = "Citation required!";
        public Article(int id, int year, string name, string citation)
        {
            Id = id;
            Year = year;
            Name = name;
            Citation = citation;
        }
    }

    public class Relation
    {
        public CC Item1 { get; set; }
        public int Item1Id { get; set; } = -1;
        public CC Item2 { get; set; }
        public int Item2Id { get; set; } = -1;
        public Char Type { get; set; }
        public int ArticleId { get; set; } = -1; // -1 is 'no evidence'
        public int Year { get; set; } = int.MaxValue; // Should just point to the article's year tbh, lets set up an indexing list for that
        // Max value to be as 'young' as possible, so that relations without evidence are generally discarded in favour of those that have evidence where applicable
        public static List<Char> TypeIndices { get; private set; } = ['>'];
        public Relation(CC item1, CC item2, char type, int artId)
        {
            Item1 = item1;
            Item2 = item2;
            Type = type;
            ArticleId = artId;
        }
        public Relation(CC item1, CC item2, char type) : this(item1, item2, type, -1)
        {
        }

        public override bool Equals(object? obj)
        {
            return obj is Relation other &&
                   Item1.Equals(other.Item1) &&
                   Item2.Equals(other.Item2) &&
                   ArticleId.Equals(other.ArticleId) &&
                   Type == other.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Item1, Item2, ArticleId, Type);
        }

        public bool IsType(string type)
        {
            if (Type.ToString() == type)
            {
                return true;
            }
            if (type == "ng")
            {
                return (Type == 'N');
            }
            return false;
        }
        public override string ToString()
        {
            return $"Relation type {Type} between {Item1} and {Item2} from article id {ArticleId}.";
        }
    }
    public class Model
    {
        public int Id { get; private set; } = -1;
        public int ArticleId { get; set; } = -1;
        public List<HashSet<CC>> CardinalValues { get; set; } = new();
        // The idea with CardinalValues is that each set in the list is an equivalence class by equipotence
        // Then if one set comes before another, the cardinals in the first set are less than those in the second
    }
}

namespace CCView.CardinalData.Compute
{
    public class RelationDatabase
    {
        public List<CC> Cardinals { get; private set; } = [];
        private HashSet<Relation> Relations = [];
        private List<Article> Articles { get; set; } = [];
        private List<Model> Models { get; set; } = [];
        private List<int> CCI { get; set; } = [];
        private List<int> ArtInds { get; set; } = [];
        private List<int> ModInds { get; set; } = [];

        public RelationDatabase(IEnumerable<CC> cardinals, HashSet<Relation> relations)
        {
            Cardinals.AddRange(cardinals);

            foreach (var relation in relations)
            {
                Relations.Add(relation);
                if (!Cardinals.Contains(relation.Item1))
                    Cardinals.Add(relation.Item1);
                if (!Cardinals.Contains(relation.Item2))
                    Cardinals.Add(relation.Item2);
            }

            // Reflexivity (not really needed)
            //foreach (var cardinal in Cardinals)
            //{
            //    Relations.Add(new Relation(cardinal, cardinal, '>'));
            //}

            // Initialise the CCIndex
            // Using this, Cardinals[CCI[i]] will return the CC with Id i
            CCI = InitIndexList<CC>(Cardinals, c =>c.Id, -1);
            ArtInds = InitIndexList<Article>(Articles, a => a.Id, -1);
            ModInds = InitIndexList<Model>(Models, m => m.Id, -1);

            foreach (Relation r in Relations)
            {
                if (r.ArticleId != -1)
                {
                    r.Year = Articles[ArtInds[r.ArticleId]].Year;
                }
            }

        }
        public RelationDatabase()
        {
        }
        
        public List<int> InitIndexList<T>(List<T> values, Func<T, int> idFinder, int defaultValue = -1)
        {
            if (values.Count == 0)
            {
                return [];
            }
            List<int> ids = values.Select(v => idFinder(v)).ToList();
            int maxId = ids.Max();
            List<int> IndexingList = [.. Enumerable.Repeat(defaultValue, maxId + 1)];
            for (int i = 0; i < maxId + 1; i++)
            {
                IndexingList[ids[i]] = i;
            }
            return IndexingList;
        }
        public static HashSet<Relation> ComputeTransitiveClosure(HashSet<Relation> relation)
        {
            HashSet<Relation> newRelation = [.. relation]; // This is short for new HashSet<Relation>(relation);
            Relation testRelation = new(new CC(-1, "Test"), new CC(-1, "Test"), '>'); // This is to save memory
            bool changed;
            do
            {
                changed = false;

                foreach (var relOne in newRelation)
                {
                    foreach (var relTwo in newRelation)
                    {
                        if (!(relOne.Type == '>' && relTwo.Type == '>')) continue;
                        testRelation.Item1 = relOne.Item1;
                        testRelation.Item2 = relTwo.Item2;
                        testRelation.Type = relOne.Type; // Trying to save memory
                        if (relOne.Item2.Equals(relTwo.Item1)
                            && !newRelation.Contains(testRelation)
                            && !relOne.Item1.Equals(relTwo.Item2))
                        {
                            newRelation.Add(new Relation(relOne.Item1, relTwo.Item2, relOne.Type));
                            changed = true;
                        }
                    }
                }
            } while (changed);
            return newRelation;
        }
        public void TransClose()
        {
            HashSet<Relation> newRels = ComputeTransitiveClosure(Relations);
            Relations = newRels;
        }

        public void AddRelation(CC? a, CC? b, char type, bool lazy = true)
        {
            if (a == null || b == null)
            {
                throw new ArgumentException("Neither cardinal may be null.");
            }
            else if (!Cardinals.Contains(a) || !Cardinals.Contains(b))
            {
                throw new ArgumentException("Both cardinals must be part of the relations.");
            }

            var toAdd = new HashSet<Relation> {
            //new Relation(a, a, '>'),
            //new Relation(b, b, '>'), // I don't think we need to enforce reflexivity
            new Relation(a, b, type)
            };

            foreach (var rel in toAdd)
            {
                Relations.Add(rel);
            }

            if (!lazy)
            {
                Relations = ComputeTransitiveClosure(Relations);
            }
        }
        public int NewId(bool fast = false)
        {
            if (fast)
            {
                return Cardinals.Count;
            }
            else
            {
                var newId = 0;
                var usedIds = new HashSet<int>(Cardinals.Select(c => c.Id));
                while (usedIds.Contains(newId))
                {
                    newId++;
                }
                return newId;
            }
        }
        public void AddCardinal(string? name, string? symbol, int id)
        {
            if (id < CCI.Count && CCI[id] != -1) // Order of operations is important here or you'll get errors for id >= CCI.Count
            {
                throw new ArgumentException($"ID {id} is in use by {GetCardinalById(id)}.");
            }
            var newCardinal = new CC(id, name, symbol);
            if (id >= CCI.Count)
            {
                CCI.AddRange(Enumerable.Repeat(-1, id + 1 - CCI.Count));
            }
            CCI[id] = Cardinals.Count; // It's important to do this before adding the cardinal
            Cardinals.Add(newCardinal);
            //Relations.Add(new Relation(newCardinal, newCardinal, '>')); // Reflexivity
            Console.WriteLine($"Added new cardinal: {newCardinal}");
        }
        public void AddCardinal(string? name, string? symbol, bool fast = false)
        {
            AddCardinal(name, symbol, NewId(fast));
        }
        public bool IsRelated(CC a, CC b, char type)
        {
            return Relations.Contains(new Relation(a, b, type));
        }
        public HashSet<Relation> GetRelations()
        {
            return Relations;
        }
        public HashSet<Relation> GetMinimalRelations(List<CC> desiredCardinals)
        {
            return GraphAlgorithm.GetMinimalRelations(Relations, desiredCardinals);
        }

        public HashSet<Relation> GetMinimalRelations()
        {
            return GetMinimalRelations(Cardinals);
        }

        public CC? GetCardinalById(int id)
        {
            CC? match = Cardinals[CCI[id]];
            if (match.Id != id)
            {
                return MisalignedCCIandC(id);
            }
            if (match != null)
            {
                return match;
            }
            else
            {
                Console.WriteLine($"WARNING: No cardinal with id {id} found. Returning null.");
                return null;
            }
        }
        public CC GetCardinalByIdOrThrow(int id)
        {
            CC? match = GetCardinalById(id);
            if (match == null)
            {
                throw new ArgumentException($"Id {id} does not belong to a cardinal characteristic.");
            }
            return match;
        }

        public void AddRelationByIds(int id1, int id2, char type)
        {
            AddRelation(GetCardinalById(id1), GetCardinalById(id2), type);
        }
        public CC GetCardinalBySymbol(string symbol)
        {
            CC? match = Cardinals.FirstOrDefault(c => c.SymbolString == symbol);
            if (match != null)
            {
                return match;
            }
            else
            {
                throw new ArgumentException($"No cardinal with symbol {symbol} found.");
            }
        }
        public CC? MisalignedCCIandC(int id)
        {
            // Before release comment the following line and uncomment the three after
            throw new InvalidOperationException("List CCI and Cardinals are mis-aligned. This is a developer error, if you see this in typical use please submit a bug report.");
            // Console.WriteLine($"WARNING: List CCI is unaligned with list Cardinals. Please submit a bug report.");
            // Console.WriteLine("Manually calling cardinal.");
            // return Cardinals.SingleOrDefault(c => c.Id == id);
        }
        private int RelAge(Relation relation)
        {
            return Articles[ArtInds[relation.ArticleId]].Year;
        }
    }
}
namespace CCView.CardinalData.QGInterface
{
    public class RelEdge : Edge<CC>
    {
        public Relation Relation { get; }
        public RelEdge(Relation relation) : base(relation.Item1, relation.Item2)
        {
            Relation = relation;
        }
        public override string ToString()
        {
            return Relation.ToString();
        }
    }
}
namespace CCView.CardinalData.JsonSaveable
{
    public class CardinalJS : JsonHandler.JsonSaveable
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "No name assigned.";
        protected override List<string> FieldsToSave => ["Id", "Name"];
        public CardinalJS(int id, string name)
        {
            Id = id;
            Name = name;
        }
        public CardinalJS() { }
        public static CardinalJS FromJson(string json)
        {
            CardinalJS newCT = new();
            List<JToken> JList = newCT.LoadFromJson(json);
            newCT.Id = (int)JList[0];
            newCT.Name = JList[1].ToString();
            return newCT;
        }
    }
    public class RelationJS : JsonHandler.JsonSaveable
    {
        public CardinalJS Item1 { get; set; } = null!;
        public CardinalJS Item2 { get; set; } = null!;
        public char Type { get; set; } = 'X';
        protected override List<String> FieldsToSave => ["Item1.Id", "Item2.Id", "Type"];
        RelationJS(CardinalJS item1, CardinalJS item2, char type)
        {
            Item1 = item1;
            Item2 = item2;
            Type = type;
        }
        public RelationJS() { }
        public static RelationJS FromJson(string json, List<CardinalJS> cardinals)
        {
            RelationJS newRJ = new();
            List<JToken> JList = newRJ.LoadFromJson(json);
            int id1 = (int)JList[0];
            int id2 = (int)JList[1];
            var item1 = cardinals[id1];
            var item2 = cardinals[id2];
            if (item1.Id != id1 || item2.Id != id2)
            {
                // Before public release we'll want to instead throw a warning and then do a manual search
                throw new ArgumentException("Cardinals list mis-indexed. Cardinal with id i must be at index i.");
            }
            newRJ.Item1 = item1;
            newRJ.Item2 = item2;
            newRJ.Type = (char)JList[2];
            return newRJ;
        }
    }
}