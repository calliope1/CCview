using QuikGraph;
using QuikGraph.Graphviz;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using CC = CardinalCharacteristic;

namespace CCView
{
    public class Program
    {
        private List<CC> Cardinals;
        private HashSet<Relation> RelationSet;
        private RelationDatabase Relations;

        static int Main(string[] args)
        {
            // Commands to add:
            // Load cardinals and relations
            // Add cardinals
            // Add relations
            // Compute closure
            // Draw graph (with certain input numbers)
            // Save everything
            // Exit
            // Default command line behaviour that puts you in a command line style environment
            // Note that outside of the command line environment you'll want to specify which files you're loading and unloading each time...
            Option<int> intOption = new("--int")
            {
                Description = "An integer why not?",
                DefaultValueFactory = parseResult => 31
            };

            Argument<string> nameArgument = new("name")
            {
                Description = "Give us a name!"
            };

            RootCommand rootCommand = new("Cardinal characteristics parser");
            rootCommand.Options.Add(intOption);

            Command nameCommand = new("name", "Description 3")
            {
                nameArgument
            };
            nameCommand.Aliases.Add("rose");

            Command intCommand = new("int", "Description 4")
            {
                intOption
            };

            rootCommand.Subcommands.Add(nameCommand);
            rootCommand.Subcommands.Add(intCommand);

            nameCommand.SetAction(parseResult => NameFunction(
                parseResult.GetValue(nameArgument)));

            intCommand.SetAction(parseResult => IntFunction(
                parseResult.GetValue(intOption)));

            rootCommand.SetAction(parseResult => NotMain(parseResult.Tokens.Select(t => t.Value).ToArray()));

            return rootCommand.Parse(args).Invoke();
        }

        internal static string NameFunction(string name)
        {
            string v = $"Your name is {name}!" ?? "Why didn't you give me a name";
            Console.WriteLine(v);
            return v;
        }

        internal static int IntFunction(int intOption)
        {
            string v = $"Your number was {intOption}!";
            Console.WriteLine(v);
            return intOption;
        }

        public static void NotMain(string[] args)
        {
            var program = new Program();

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string[] tokens = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = tokens[0].ToLower();
                string[] arguments = tokens.Skip(1).ToArray();

                switch (command)
                {
                    case "exit":
                        Console.WriteLine("Exiting...");
                        return;
                    case "quit":
                        Console.WriteLine("Exiting...");
                        return;
                    case "help":
                        Console.WriteLine("exit, help, save");
                        break;
                    case "save":
                        Console.WriteLine("Saving relations and cardinals...");
                        JsonInterface.SaveRelations(program.Relations.GetRelations());
                        JsonInterface.SaveCardinals(program.Relations.Cardinals);
                        break;
                    case "create":
                        if (arguments.Length < 1)
                        {
                            Console.WriteLine("Usage: create <name>");
                            continue;
                        }
                        program.Relations.AddCardinal(string.Join(" ", arguments));
                        break;
                    case "relate":
                        Console.WriteLine("To do");
                        break;
                    case "plot":
                        var dot = GraphDrawer.DrawRelationDot(program.Relations.GetMinimalRelations());
                        GraphDrawer.WriteDotFile(dot, Program.GetOutputPath(), "relations.dot", "graph.png");
                        break;
                    default:
                        Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
                        break;
                }
            }
        }
        public Program()
        {
            Cardinals = JsonInterface.LoadCardinals();
            RelationSet = JsonInterface.LoadRelations(Cardinals);
            Relations = new RelationDatabase(Cardinals, RelationSet);
            JsonInterface.SaveRelations(Relations.GetRelations());

            //for (int i = 0; i < Cardinals.Count; i++)
            //{
            //    Console.WriteLine("{0} {1}", Cardinals[i].Name, Cardinals[i].Id);
            //}
            //foreach (CC item in Cardinals)
            //{
            //    Console.WriteLine("{0} {1}", item.Name, item.Id);
            //}
            //var dot = GraphDrawer.DrawRelationDot(Relations.GetMinimalRelations());
            //GraphDrawer.WriteDotFile(dot, GetOutputPath(), "relations.dot", "graph.png");
            //Console.Write(Relations.GetMinimalRelations().Count());
        }

        public static string GetOutputPath(string filename)
        {
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"../../../"));
            return Path.Combine(projectRoot, "output", filename);
        }

        public static string GetOutputPath() => GetOutputPath("");

    }
}