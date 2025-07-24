using QuikGraph;
using QuikGraph.Graphviz;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardinalData;
using CardinalData.Compute;
using CC = CardinalData.CardinalCharacteristic;

namespace CCView.GraphLogic
{
    public class GraphHandler
    {
        public static (HashSet<Relation>, IEnumerable<CC>) CleanRelations(HashSet<Relation> relations, List<CC> cardinals)
        {
            var cleanRelations = new HashSet<Relation>();

            foreach (Relation rel in relations)
            {
                if (rel.Item1 == null || rel.Item2 == null)
                {
                    throw new ArgumentException("Relation contains null cardinal characteristic.");
                }
                //else if (!rel.IsType(">") && !rel.IsType("ng"))
                else if (rel.Type != '>')
                {
                    continue;
                }
                else cleanRelations.Add(rel);
            }

            var allVertices = cleanRelations
                .SelectMany(rel => new[] { rel.Item1, rel.Item2 })
                .Concat(cardinals)
                .Distinct();
            return (cleanRelations, allVertices);
        }
    }
}

namespace CCView.GraphLogic.Vis
{
    public class GraphDrawer
    {
        public static string GenerateGraph(HashSet<Relation> relations, List<CC> cardinals)
        {
            var graph = new AdjacencyGraph<CC, Edge<CC>>();

            var algorithm = new GraphvizAlgorithm<CC, Edge<CC>>(graph);

            algorithm.FormatVertex += (sender, args) =>
            {
                args.VertexFormat.Label = args.Vertex.SymbolString;
            };

            var (cleanRelations, allVertices) = GraphHandler.CleanRelations(relations, cardinals);

            graph.AddVertexRange(allVertices);

            foreach (var rel in cleanRelations)
            {
                graph.AddEdge(new Edge<CC>(rel.Item1, rel.Item2));
            }

            var graphviz = new GraphvizAlgorithm<CC, Edge<CC>>(graph);
            //{
            //    graphviz.FormatVertex += (vertex, writer) =>
            //    {
            //        writer.WriteAttribute("label", vertex.Name);
            //        writer.WriteAttribute("shape", "ellipse");
            //    };
            //}

            return algorithm.Generate(); // Why does this not have to take graph as an input?
        }

        public static void WriteDotFile(string dot, string outputDotPath, string outputFileName)
        {
            File.WriteAllText(Path.Combine(outputDotPath, outputFileName), dot);

            Console.WriteLine($"DOT file written to {outputDotPath}\"{outputFileName}");
            //Console.WriteLine("You can render it using Graphviz:");
            //Console.WriteLine($"> dot -Tpng \"{outputDotPath}\\{outputFileName}\" -o graph.png");
        }

        public static void WriteDotFile(string dot, string outputDotPath)
        {
            File.WriteAllText(outputDotPath, dot);
            Console.WriteLine($"DOT file written to {outputDotPath}");
        }

        public static void WritePngFile(string dotFilePath, string dotFileName, string outputFilePath, string outputFileName)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dot",
                    Arguments = $"-Tpng \"{Path.Combine(dotFilePath, dotFileName)}\" -o \"{Path.Combine(outputFilePath, outputFileName)}\"",
                    WorkingDirectory = outputFilePath,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();

            Console.WriteLine($"Graph image generated as {Path.Combine(outputFilePath, outputFileName)}");
        }
    }
}

namespace CCView.GraphLogic.Algorithms
{
    public class GraphAlgorithm
    {
        public static HashSet<Relation> GetMinimalRelations(HashSet<Relation> relations, List<CC> desiredCardinals)
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
                    if (relations.Contains(testRelation))
                    {
                        testRelation.Item1 = d;
                        testRelation.Item2 = c;
                        if (relations.Contains(testRelation))
                        {
                            toAdd = false;
                            break;
                        }
                    }
                }
                if (toAdd) minimalCardinals.Add(c);
            }

            foreach (var rel in relations)
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
                        if (relations.Contains(testRelation))
                        {
                            testRelation.Item1 = c;
                            testRelation.Item2 = rel.Item2;
                            if (relations.Contains(testRelation))
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
        public static HashSet<Relation> GetMinimalRelations(RelationDatabase rD)
        {
            return GetMinimalRelations(rD.GetRelations(), rD.Cardinals);
        }

        public static List<Relation> OldestPath(HashSet<Relation> relations, CC c1, CC c2)
        {
            var graph = new AdjacencyGraph<CC, Edge<CC>>();

            // delete the following
            return [];
        }

        //public static HashSet<Relation> GetOldestSpan(HashSet<Relation> relations, List<CC> cardinals)
        //{
        //    HashSet<(CC, CC)> pureRels = relations.Select(r => (r.Item1, r.Item2)).ToHashSet();
        //    // Delete the following line
        //    return new();
        //}
    }
}