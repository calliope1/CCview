using QuikGraph;
using QuikGraph.Graphviz;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph.Algorithms;
using CCview.Core.DataClasses;
using CCview.Core.Services;

namespace CCview.Core.GraphLogic
{
    public class GraphHandler
    {
        // Return a minimal collection of relations (of Type type) that involve only those CCs in cardinals
        public static Dictionary<int, Relation> OldestCtoCMinimalSample(IEnumerable<CC> cardinals, IEnumerable<Relation> relations, char symbol)
        {
            IEnumerable<int> validIds = cardinals.Select(c => c.Id);
            if (!RelationType.IsCtoC(symbol)) throw new ArgumentException($"Type {symbol} is not valid for CtoC operations.");
            Dictionary<(int, int), Relation> agedRels = [];
            foreach (Relation r in relations)
            {
                if (r.Relationship.Symbol != symbol
                    || !validIds.Contains(r.Item1Id)
                    || !validIds.Contains(r.Item2Id))
                {
                    continue;
                }
                if (agedRels.TryGetValue((r.Item1Id, r.Item2Id), out Relation? other))
                {
                    if (r.Birthday < other.Birthday || other == null)
                    {
                        agedRels[(r.Item1Id, r.Item2Id)] = r;
                    }
                }
                else
                {
                    agedRels[(r.Item1Id, r.Item2Id)] = r;
                }
            }
            return agedRels.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Value);
        }
        // This is not fast, but it does do the job
        public static HashSet<HashSet<CC>> EquivalenceClasses(IReadOnlyDictionary<int, CC> cardinals, Dictionary<int, Relation> relations)
        {
            Dictionary<int, HashSet<int>> teamNames = [];
            IEnumerable<int> validIds = cardinals.Select(c => c.Key);
            foreach (int id in validIds) teamNames[id] = [id];
            foreach (Relation r1 in relations.Values)
            {
                if (r1.Relationship.Symbol.Equals('='))
                {
                    if (!validIds.Contains(r1.Item1Id) || !validIds.Contains(r1.Item2Id)) { continue; }
                    teamNames[r1.Item1Id].UnionWith(teamNames[r1.Item2Id]);
                    teamNames[r1.Item2Id].UnionWith(teamNames[r1.Item1Id]);
                    continue;
                }
                else if (r1.Relationship.Symbol.Equals('>'))
                {
                    foreach (Relation r2 in relations.Values)
                    {
                        if (r2.Relationship.Symbol.Equals('>')
                            && r1.Item1Id.Equals(r2.Item2Id)
                            && r1.Item2Id.Equals(r2.Item1Id))
                        {
                            teamNames[r1.Item1Id].UnionWith(teamNames[r1.Item2Id]);
                            teamNames[r2.Item1Id].UnionWith(teamNames[r2.Item2Id]);
                        }
                    }
                }
            }
            HashSet<HashSet<CC>> classes = [];
            foreach (HashSet<int> team in teamNames.Values)
            {
                HashSet<CC> equivalenceClass = [];
                foreach (int id in team)
                {
                    if (cardinals.TryGetValue(id, out CC? value))
                    {
                        equivalenceClass.Add(value);
                    }
                }
                if (classes.Any(c => c.SetEquals(equivalenceClass))) { continue; }
                classes.Add(equivalenceClass);
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
    public class GraphDrawer
    {
        public static string GenerateGraph(Dictionary<int, CC> cardinals, HashSet<Relation> relations, RelationDatabase rd)
        {
            Dictionary<int, Relation> cleanRelations = GraphHandler.OldestCtoCMinimalSample(cardinals.Values, relations, '>');
            HashSet<HashSet<CC>> cardinalClasses = GraphHandler.EquivalenceClasses(cardinals, cleanRelations);
            IEnumerable<CC> allVertices = cardinalClasses.Select(eClass => eClass.First());
            var graph = GraphHandler.CCRGraph(allVertices, cleanRelations.Values, rd);
            var algorithm = new GraphvizAlgorithm<CC, RelEdge>(graph);

            algorithm.FormatVertex += (sender, args) =>
            {
                args.VertexFormat.Label = args.Vertex.EquationSymbol;
            };

            graph.AddVertexRange(allVertices);
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

        public static void WritePngFile(string dotFilePath, string dotFileName, string outputFilePath, string outputFileName, string dotArgument)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dot",
                    Arguments = $"{dotArgument} \"{Path.Combine(dotFilePath, dotFileName)}\" -o \"{Path.Combine(outputFilePath, outputFileName)}\"",
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
    public class RelEdge : Edge<CC>
    {
        public Relation Relation { get; }
        public RelEdge(Relation relation, RelationDatabase rd) : base(rd.GetCardinalById(relation.Ids[0])!, rd.GetCardinalById(relation.Ids[1])!)
        {
            Relation = relation;
        }
        public override string ToString()
        {
            return Relation.ToString();
        }
    }
    public static class GraphAlgorithm
    {
        public static HashSet<Relation> TransitiveReduction(IReadOnlyDictionary<int, CC> desiredCardinals, Dictionary<int, Relation> relations)
        {
            Sentence testSentence = new('>', [-1, -1]);
            HashSet<Relation> minimalRelations = [];
            HashSet<HashSet<CC>> equivalenceClasses = GraphHandler.EquivalenceClasses(desiredCardinals, relations);
            HashSet<CC> minimalCardinals = equivalenceClasses.Select(eClass => eClass.First()).ToHashSet();
            HashSet<int> minimalIds = minimalCardinals.Select(c => c.Id).ToHashSet();
            HashSet<Sentence> sentences = relations.Select(r => r.Value.Statement).ToHashSet();

            foreach (var rel in relations.Values)
            {
                if (!rel.Relationship.Symbol.Equals('>')
                    || rel.Item1Id.Equals(rel.Item2Id)
                    || !minimalIds.Contains(rel.Item1Id)
                    || !minimalIds.Contains(rel.Item2Id)) continue;
                bool toAdd = true;
                // We have rel = ``rel.Item1Id > rel.Item2Id``
                // Now loop over minimalIds to see if there is both ``id > rel.Item2Id`` and ``rel.Item1Id > id``
                // If so, throw out rel, since it's not minimal
                foreach (int id in minimalIds)
                {
                    if (id.Equals(rel.Item1Id) || id.Equals(rel.Item2Id)) continue;
                    testSentence = new('>', [rel.Item1Id, id]);
                    if (sentences.Contains(testSentence))
                    {
                        testSentence = new('>', [id, rel.Item2Id]);
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
        public static HashSet<Relation> DensityTransitiveReduction(IReadOnlyDictionary<int, CC> cardinals, Dictionary<int, Relation> relations, Dictionary<(int, int), HashSet<int>> density)
        {
            HashSet<Relation> minimalRelations = [];
            foreach (Relation r in relations.Values)
            {
                if (!r.Relationship.Symbol.Equals('>')) continue;
                // If density[(a, b)] is empty then it won't even be a key
                if (density.TryGetValue((r.Item1Id, r.Item2Id), out HashSet<int>? inBetween)
                    && inBetween.Intersect(cardinals.Values.Select(cardinal => cardinal.Id)).Any()) continue;
                minimalRelations.Add(r);
            }
            return minimalRelations;
        }
    }
}