using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CardinalData;
using CardinalData.Compute;
using CC = CardinalData.CardinalCharacteristic;
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
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                List<CC>? items = JsonConvert.DeserializeObject<List<CC>>(json); // The ? here tells me that it could be null, and that's ok
                return items ?? new List<CC>();
            }
        }
        public static List<CC> LoadCardinals() // Temporary, delete when not needed
        {
            return LoadCardinals(GetAssetsPath("cardinal_characteristics.json"));
        }
        public static void SaveCardinals(String path, List<CC> cardinals)
        {
            string json = JsonConvert.SerializeObject(cardinals, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        public static void SaveCardinals(List<CC> cardinals) // Temp
        {
            SaveCardinals(GetAssetsPath("cardinal_characteristics.json"), cardinals);
        }


        public static HashSet<Relation> LoadRelations(string path, List<CC> cardinals) // Only loads the relations for the listed cardinals
        {
            // Add catches for if the entries don't have an id in the list of cardinals
            var byId = cardinals.ToDictionary(c => c.Id);

            using (StreamReader r = new StreamReader(path))
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
        public static HashSet<Relation> LoadRelations(List<CC> cardinals) // Temporary, delete when no longer needed
        {
            return LoadRelations(GetAssetsPath("relations.json"), cardinals);
        }
        public static void SaveRelations(String path, HashSet<Relation> relations)
        {
            var simplified = relations.Select(rel => new[] {
                rel.Item1.Id,
                rel.Item2.Id,
                RelationTypeToInt(rel.Type),
                rel.ArticleId,
                rel.Year
            }).ToList();
            string json = JsonConvert.SerializeObject(simplified, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        public static void SaveRelations(HashSet<Relation> relations) // Again temp
        {
            SaveRelations(GetAssetsPath("relations.json"), relations);
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
}