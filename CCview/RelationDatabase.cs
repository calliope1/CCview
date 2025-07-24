using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CardinalData;
using CardinalData.Compute;
using CCView.GraphLogic.Algorithms;
using CC = CardinalData.CardinalCharacteristic;
//using Microsoft.Extensions.ObjectPool;

namespace CardinalData
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
        public int Id { get; private set; }
        public int Year { get; private set; }
        public string Name { get; private set; } = "Article name required!";
        public string Citation { get; private set; } = "Citation required!";
        public Article(int id, int year, string name, string citation)
        {
            Id = id;
            Year = year;
            Name = name;
            Citation = citation;
        }
        public Article()
        {
            Id = 0;
            Year = 0;
        }
    }

    public class Relation
    {
        public CC Item1 { get; set; }
        public CC Item2 { get; set; }
        public Char Type { get; set; }
        public int ArticleId { get; set; } = -1; // -1 is 'no evidence'
        public int Year { get; set; } = 0; // Should just point to the article's year tbh, lets set up an indexing list for that
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
            int hash = 13;
            hash = hash * 17 + Item1.GetHashCode();
            hash = hash * 23 + Item2.GetHashCode();
            hash = hash * 29 + ArticleId.GetHashCode();
            hash = hash * 31 + Type.GetHashCode();
            return hash;
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
    }
    public class Model
    {
        public int ArticleId { get; set; } = -1;
        public List<HashSet<CC>> CardinalValues { get; set; } = new();
        // The idea with CardinalValues is that each set in the list is an equivalence class by equipotence
        // Then if one set comes before another, the cardinals in the first set are less than those in the second
    }
}

namespace CardinalData.Compute
{
    public class RelationDatabase
    {
        public List<CC> Cardinals { get; private set; } = new();
        private HashSet<Relation> Relations = new();
        private List<int> CCI { get; set; } = new();

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
            List<int> ids = Cardinals.Select(c => c.Id).ToList();
            int maxId = ids.Max();
            CCI = [.. Enumerable.Repeat(-1, maxId + 1)]; // This means Enumerable.Repeat(...).ToList();
            for (int i = 0; i < maxId + 1; i++)
            {
                CCI[ids[i]] = i;
            }
        }
        public RelationDatabase()
        {
        }
        public static HashSet<Relation> ComputeTransitiveClosure(HashSet<Relation> relation)
        {
            HashSet<Relation> newRelation = [.. relation]; // This is short for new HashSet<Relation>(relation);
            Relation testRelation = new(new CC(-1, "Test"), new CC(-1, "Test"), '>');
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
    }
}