using QuikGraph;
using QuikGraph.Graphviz;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CC = CCView.CardinalData.CardinalCharacteristic;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Search;
using CCView.CardinalData.QGInterface;

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
        public static AdjacencyGraph<CC, RelEdge> CCRGraph(IEnumerable<CC> cardinals, IEnumerable<Relation> relations)
        {
            AdjacencyGraph<CC, RelEdge> graph = new();
            var newEdges = relations.Select(r => new RelEdge(r));
            graph.AddVertexRange(cardinals);
            graph.AddEdgeRange(newEdges);
            return graph;
        }
    }
}

namespace CCView.GraphLogic.Vis
{
    public class GraphDrawer
    {
        public static string GenerateGraph(HashSet<Relation> relations, List<CC> cardinals)
        {
            var (cleanRelations, allVertices) = GraphHandler.CleanRelations(relations, cardinals);
            var graph = GraphHandler.CCRGraph(allVertices, cleanRelations);
            var algorithm = new GraphvizAlgorithm<CC, RelEdge>(graph);

            algorithm.FormatVertex += (sender, args) =>
            {
                args.VertexFormat.Label = args.Vertex.SymbolString;
            };

            graph.AddVertexRange(allVertices);
            //graph.AddEdgeRange(cleanRelations.Select(r => new RelEdge(r)));
            GraphvizAlgorithm<CC, RelEdge>  graphviz = new(graph);

            return algorithm.Generate();
        }

        public static void WriteDotFile(string dot, string outputDotPath, string outputFileName)
        {
            File.WriteAllText(Path.Combine(outputDotPath, outputFileName), dot);

            Console.WriteLine($"DOT file written to {outputDotPath}\"{outputFileName}");
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
    public static class GraphAlgorithm
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
        // The whole GetMinMaxRelations routine was made mostly by ChatGPT so I need to go through it and understand it and also agree with it
        public static HashSet<Relation> GetMinMaxMinimalRelations(HashSet<Relation> relations, List<CC> cardinals)
        {
            HashSet<Relation> result = [];
            Dictionary<int, HashSet<Relation>> AgeDict = [];
            List<int> AgeList = [];
            foreach (Relation r in relations)
            {
                if (AgeDict.Keys.Contains(r.Year))
                {
                    AgeDict[r.Year].Add(r);
                }
                else
                {
                    AgeList.Add(r.Year);
                    AgeDict.Add(r.Year, [r]);
                }
            }
            _ = AgeList.Order(); // This means AgeList = AgeList.Order();
            List<Relation> subRelations = [];
            foreach (int age in AgeList)
            {
                _ = subRelations.Union(AgeDict[age]);
                AdjacencyGraph<CC, RelEdge> graph = GraphHandler.CCRGraph(cardinals, subRelations);
                // var subGraph = BuildAdjacencyList(cardinals, subRelations);
                // var reach = ComputeReachability(cardinals, subGraph);

                TransitiveReductionAlgorithm<CC, RelEdge> algorithm = new(graph);
                algorithm.Compute();
                List<Relation> reducedEdges = (List<Relation>)graph.Edges.Select(rE => rE.Relation);

                // var reducedEdges = TransitiveReduction(cardinals, subGraph, reach, subRelations);
                _ = result.Union(reducedEdges.Intersect(AgeDict[age]));
            }
            return result;
        }
        public static Dictionary<CC, List<CC>> BuildAdjacencyList(List<CC> cardinals, List<Relation> relations)
        {
            Dictionary<CC, List<CC>> adj = cardinals.ToDictionary(c => c, c => new List<CC>());
            foreach (Relation r in relations)
            {
                adj[r.Item1].Add(r.Item2);
            }
            return adj;
        }
        public static Dictionary<CC, HashSet<CC>> ComputeReachability(List<CC> cardinals, Dictionary<CC, List<CC>> adjacency)
        {
            Dictionary<CC, HashSet<CC>> reach = new();
            foreach (CC c in cardinals)
            {
                HashSet<CC> visited = [];
                // DepthFirstSearchAlgorithm // FIGURE OUT HOW THIS WORKS TOO
                DFS(c, adjacency, visited);
                reach[c] = visited;
            }
            return reach;
        }
        public static void DFS(CC c, Dictionary<CC, List<CC>> adjacency, HashSet<CC> visited)
        {
            Stack<CC> stack = new();
            stack.Push(c);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (visited.Add(current)) // Don't like this :\
                {
                    foreach (CC neighbour in adjacency[current])
                    {
                        stack.Push(neighbour);
                    }
                }
            }
        }
        public static List<Relation> TransitiveReduction(
            List<CC> cardinals,
            Dictionary<CC, List<CC>> adjacency,
            Dictionary<CC, HashSet<CC>> reach,
            List<Relation> relations
            )
        {
            List<Relation> reduced = [];
            foreach (CC c in cardinals)
            {
                foreach (CC d in adjacency[c])
                {
                    bool redundant = false;
                    foreach (CC intermediate in adjacency[c])
                    {
                        if (!intermediate.Equals(d) && reach[intermediate].Contains(d))
                        {
                            redundant = true;
                            break;
                        }
                    }
                    if (!redundant)
                    {
                        reduced.Add(FindOldestRelation(c, d, relations));
                    }
                }
            }
            return reduced;
        }
        public static Relation FindOldestRelation(CC c, CC d, List<Relation> relations)
        {
            int minAge = int.MinValue;
            Relation? result = null;
            foreach (Relation r in relations)
            {
                if (r.Item1.Equals(c) && r.Item2.Equals(d) && r.Year < minAge)
                {
                    minAge = r.Year;
                    result = r;
                }
            }
            if (result != null)
            {
                return result;
            }
            else
            {
                throw new ArgumentException($"No relation between {c} and {d} exists in the provided list.");
            }
        }

        public static List<Relation> OldestPath(HashSet<Relation> relations, CC c1, CC c2)
        {
            // This is a co-Widest path algorithm, finding the combination of Relations with the least maximum Year.

            Console.WriteLine("NOT YET IMPLEMENTED.");

            return [];
        }
    }
}