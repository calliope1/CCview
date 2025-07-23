using QuikGraph;
using QuikGraph.Graphviz;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CC = CardinalCharacteristic;

public class GraphDrawer
{
    // To do add lonesome cardinals without arrows pointing to themselves
    public static string DrawRelationDot(HashSet<Relation> relations, List<CC> cardinals)
    {
        var graph = new AdjacencyGraph<CC, Edge<CC>>();

        var cleanRelations = new HashSet<Relation>();

        foreach (Relation rel in relations)
        {
            if (rel.Item1 == null || rel.Item2 == null)
            {
                throw new ArgumentException("Relation contains null cardinal characteristic.");
            }
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
        graph.AddVertexRange(cardinals);

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

        string dot = graphviz.Generate();
        return dot;
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