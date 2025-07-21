using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CC = CardinalCharacteristic;
//using Microsoft.Extensions.ObjectPool;

public class JsonInterface
{
    public static string GetAssetsPath(string filename)
    {
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"../../../"));
        return Path.Combine(projectRoot, "assets", filename);
    }
    public static List<CC> LoadCardinals()
    {
        using (StreamReader r = new StreamReader(GetAssetsPath("cardinal_characteristics.json")))
        {
            string json = r.ReadToEnd();
            List<CC>? items = JsonConvert.DeserializeObject<List<CC>>(json); // The ? here tells me that it could be null, and that's ok
            return items ?? new List<CC>();
        }
    }
    public static void SaveCardinals(List<CC> cardinals)
    {
        string json = JsonConvert.SerializeObject(cardinals, Formatting.Indented);
        File.WriteAllText(GetAssetsPath("cardinal_characteristics.json"), json);
    }

    public static HashSet<Relation> LoadRelations(List<CC> cardinals) // Only loads the relations for the listed cardinals
    {
        // Add catches for if the entries don't have an id in the list of cardinals
        var byId = cardinals.ToDictionary(c => c.Id);

        using (StreamReader r = new StreamReader(GetAssetsPath("relations.json")))
        {
            string json = r.ReadToEnd();
            var relationTuples = JsonConvert.DeserializeObject<List<int[]>>(json) ?? [];
            var result = new HashSet<Relation>();
            foreach (var tup in relationTuples)
            {
                if (tup.Length != 3 || !byId.ContainsKey(tup[0]) || !byId.ContainsKey(tup[1]))
                {
                    continue; // Optionally log
                }
                result.Add(new Relation(byId[tup[0]], byId[tup[1]], IntToRelationType(tup[2])));
            }

            return result;
        }
    }
    public static void SaveRelations(HashSet<Relation> relations)
    {
        var simplified = relations.Select(rel => new[] { rel.Item1.Id, rel.Item2.Id, RelationTypeToInt(rel.Type) }).ToList();
        string json = JsonConvert.SerializeObject(simplified, Formatting.Indented);
        File.WriteAllText(GetAssetsPath("relations.json"), json);
    }
    private static int RelationTypeToInt(char type)
    {
        if (type == '>') return 0;
        else throw new ArgumentException("Invalid relation type");
    }
    private static char IntToRelationType(int type)
    {
        if (type == 0) return '>';
        else throw new ArgumentException("Invalid relation id");
    }
}

public class CardinalCharacteristic
{
    public int Id { get; set; }
    public string Name { get; set; } = "No name assigned";

    [JsonConstructor] // Telling Json.NET to use this constructor
    public CardinalCharacteristic(int id, string name)
    {
        Id = id;
        Name = name ?? "No name assigned";
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

public class Relation
{
    public CC Item1 { get; set; }
    public CC Item2 { get; set; }
    public Char Type { get; set; }
    public Relation(CC item1, CC item2, char type)
    {
        Item1 = item1;
        Item2 = item2;
        Type = type;
    }

    public override bool Equals(object? obj)
    {
        return obj is Relation other &&
               Item1.Equals(other.Item1) &&
               Item2.Equals(other.Item2) &&
               Type == other.Type;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + Item1.GetHashCode();
        hash = hash * 29 + Item2.GetHashCode();
        hash = hash * 31 + Type.GetHashCode();
        return hash;
    }
}

public class RelationDatabase
{
    public List<CC> Cardinals { get; private set; } = new List<CC>();
    private HashSet<Relation> Relations = new HashSet<Relation>();
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

        // Reflexivity
        foreach (var cardinal in Cardinals)
        {
            Relations.Add(new Relation(cardinal, cardinal, '>'));
        }
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
                    if (relOne.Item2.Equals(relTwo.Item1) && !newRelation.Contains(testRelation))
                    {
                        newRelation.Add(new Relation(relOne.Item1, relTwo.Item2, relOne.Type));
                        changed = true;
                    }
                }
            }
        } while (changed);
        return newRelation;
    }

    public void AddRelation(CC a, CC b, char type, bool lazy = false)
    {
        if (!Cardinals.Contains(a) || !Cardinals.Contains(b))
        {
            throw new ArgumentException("Both cardinals must be part of the relations.");
        }

        var toAdd = new HashSet<Relation> {
        new Relation(a, a, '>'),
        new Relation(b, b, '>'),
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

    public void AddCardinal(string name)
    {
        // Improve this later
        int newId = Cardinals.Count > 0 ? Cardinals.Max(c => c.Id) + 1 : 0; // Generate a new ID
        var newCardinal = new CC(newId, name);
        Cardinals.Add(newCardinal);
        Relations.Add(new Relation(newCardinal, newCardinal, '>')); // Reflexivity
        Console.WriteLine($"Added new cardinal: {newCardinal}");
    }
    public bool IsRelated(CC a, CC b, char type)
    {
        return Relations.Contains(new Relation(a, b, type));
    }
    public HashSet<Relation> GetRelations()
    {
        return Relations;
    }

    public HashSet<Relation> GetMinimalRelations()
    {
        return GetMinimalRelations(Cardinals.ToHashSet());
    }
    public HashSet<Relation> GetMinimalRelations(HashSet<CC> desiredCardinals)
    {
        var testRelation = new Relation(new CC(-1, "Test"), new CC(-1, "Test"), '>');
        var minimalRelations = new HashSet<Relation>();
        var minimalCardinals = new HashSet<CC>();

        // Eliminate equivalence classes
        // To do: Include this in return
        foreach (var c in desiredCardinals)
        {
            bool toAdd = true;
            foreach (var d in minimalCardinals)
            {
                testRelation.Item1 = c;
                testRelation.Item2 = d;
                if (Relations.Contains(testRelation))
                {
                    testRelation.Item1 = d;
                    testRelation.Item2 = c;
                    if (Relations.Contains(testRelation))
                    {
                        toAdd = false;
                        break;
                    }
                }
            }
            if (toAdd) minimalCardinals.Add(c);
        }

        foreach (var rel in Relations)
        {
            if (rel.Item1.Equals(rel.Item2) || !desiredCardinals.Contains(rel.Item1) || !desiredCardinals.Contains(rel.Item2)) continue;
            else
            {
                bool toAdd = true;
                foreach (var c in desiredCardinals)
                {
                    if (c.Equals(rel.Item1) || c.Equals(rel.Item2))
                    {
                        continue; // Skip self-comparisons
                    }
                    testRelation.Item1 = rel.Item1;
                    testRelation.Item2 = c;
                    if (Relations.Contains(testRelation))
                    {
                        testRelation.Item1 = c;
                        testRelation.Item2 = rel.Item2;
                        if (Relations.Contains(testRelation))
                        {
                            toAdd = false;
                            break;
                        }
                    }
                }
                if (toAdd)
                {
                    minimalRelations.Add(rel);
                }
            }
        }
        return minimalRelations;
    }
}