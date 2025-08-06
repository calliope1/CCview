using QuikGraph;
using QuikGraph.Graphviz;
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CC = CCView.CardinalData.CardinalCharacteristic;
using QuikGraph.Algorithms;
using CCView.CardinalData.QGInterface;

namespace CCView.GraphLogic
{
    public class GraphHandler
    {
        // Return a minimal collection of relations (of Type type) that involve only those CCs in cardinals
        public static HashSet<Relation> OldestCtoCMinimalSample(IEnumerable<CC> cardinals, IEnumerable<Relation> relations, char type)
        {
            IEnumerable<int> validIds = cardinals.Select(c => c.Id);
            if (!Sentence.CtoCTypes.Contains(type)) throw new ArgumentException($"Type {type} is not valid for CtoC operations.");
            Dictionary<(int, int), Relation> agedRels = [];
            foreach (Relation r in relations)
            {
                if (r.Type != type
                    || !validIds.Contains(r.Statement.GetItem1())
                    || !validIds.Contains(r.Statement.GetItem2()))
                {
                    continue;
                }
                if (agedRels.TryGetValue((r.Statement.GetItem1(), r.Statement.GetItem2()), out Relation? other))
                {
                    if (r.Age < other.Age || other == null)
                    {
                        agedRels[(r.Statement.GetItem1(), r.Statement.GetItem2())] = r;
                    }
                }
                else
                {
                    agedRels[(r.Statement.GetItem1(), r.Statement.GetItem2())] = r;
                }
            }
            return agedRels.Values.ToHashSet();
        }
        // This is not fast, but it does do the job
        public static HashSet<HashSet<CC>> EquivalenceClasses(Dictionary<int, CC> cardinals, IEnumerable<Relation> relations)
        {
            Dictionary<int, HashSet<int>> teamNames = [];
            IEnumerable<int> validIds = cardinals.Select(c => c.Key);
            foreach (int id in validIds) teamNames[id] = [id];
            foreach (Relation r1 in relations)
                foreach (Relation r2 in relations)
                {
                    if (r1.Type == '>'
                        && r2.Type == '>'
                        && r1.Statement.GetItem1().Equals(r2.Statement.GetItem2())
                        && r1.Statement.GetItem2().Equals(r2.Statement.GetItem1()))
                    {
                        teamNames[r1.Statement.GetItem1()].Add(r1.Statement.GetItem2());
                        teamNames[r2.Statement.GetItem1()].Add(r2.Statement.GetItem2());
                    }
                }
            HashSet<HashSet<CC>> classes = [];
            foreach (HashSet<int> team in teamNames.Values)
            {
                classes.Add(team.Select(i => cardinals[i]).ToHashSet());
            }
            return classes;
        }
        public static AdjacencyGraph<CC, RelEdge> CCRGraph(IEnumerable<CC> cardinals, IEnumerable<Relation> relations, RelationDatabase rd)
        {
            AdjacencyGraph<CC, RelEdge> graph = new();
            var newEdges = relations.Select(r => new RelEdge(r, rd));
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
        public static string GenerateGraph(Dictionary<int, CC> cardinals, HashSet<Relation> relations, RelationDatabase rd)
        {
            HashSet<Relation> cleanRelations = GraphHandler.OldestCtoCMinimalSample(cardinals.Values, relations, '>');
            HashSet<HashSet<CC>> cardinalClasses = GraphHandler.EquivalenceClasses(cardinals, cleanRelations);
            HashSet<CC> allVertices = cardinalClasses.Select(eClass => eClass.First()).ToHashSet();
            var graph = GraphHandler.CCRGraph(allVertices, cleanRelations, rd);
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
        public static HashSet<Relation> TransitiveReduction(Dictionary<int, CC> desiredCardinals, IEnumerable<Relation> relations)
        {
            Sentence testSentence = new('>', [-1, -1]);
            HashSet<Relation> minimalRelations = [];
            HashSet<HashSet<CC>> equivalenceClasses = GraphHandler.EquivalenceClasses(desiredCardinals, relations);
            HashSet<CC> minimalCardinals = equivalenceClasses.Select(eClass => eClass.First()).ToHashSet();
            HashSet<int> minimalIds = minimalCardinals.Select(c => c.Id).ToHashSet();
            HashSet<Sentence> sentences = relations.Select(r => r.Statement).ToHashSet();

            foreach (var rel in relations)
            {
                if (rel.Type != '>'
                    || rel.Statement.GetItem1().Equals(rel.Statement.GetItem2())
                    || !minimalIds.Contains(rel.Statement.GetItem1())
                    || !minimalIds.Contains(rel.Statement.GetItem2())) continue;
                bool toAdd = true;
                foreach (int id in minimalIds)
                {
                    if (id.Equals(rel.Statement.GetItem1()) || id.Equals(rel.Statement.GetItem2())) continue;
                    testSentence.Ids = [rel.Statement.GetItem1(), id];
                    if (sentences.Contains(testSentence))
                    {
                        testSentence.Ids = [id, rel.Statement.GetItem2()];
                        if (sentences.Contains(testSentence))
                        {
                            toAdd = false;
                            break;
                        }
                    }
                }
                if (toAdd) minimalRelations.Add(rel);
            }
            return minimalRelations;
        }
        public static HashSet<Relation> DensityTransitiveReduction(Dictionary<int, CC> cardinals, IEnumerable<Relation> relations, Dictionary<(CC, CC), HashSet<CC>> density)
        {
            HashSet<Relation> minimalRelations = [];
            foreach (Relation r in relations)
            {
                if (r.Type != '>') continue;
                // If density[(a, b)] is empty then it won't even be a key
                if (density.TryGetValue((cardinals[r.Statement.GetItem1()], cardinals[r.Statement.GetItem2()]), out HashSet<CC>? inBetween)
                    && inBetween.Intersect(cardinals.Values).Any()) continue;
                minimalRelations.Add(r);
            }
            return minimalRelations;
        }
    }
}