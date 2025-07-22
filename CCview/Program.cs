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
        static int Main(string[] args)
        {
            // For now we assume that we are only ever working with one set of files: cardinal_characteristics.json and relations.json
            var Env = new RelationEnvironment();

            RootCommand rootCommand = new("Cardinal characteristics visualiser!");

            // Template
            //Option<string> optionOption = new("--string-option", "-so")
            //{
            //    Description = "Description of option option.",
            //    DefaultValueFactory = parseResult => "Default value."
            //};

            //Argument<string> argumentArgument = new("argument")
            //{
            //    Description = "Description of argument argument.",
            //    DefaultValueFactory = parseResult => "Not bill."
            //};

            //Command commandCommand = new("command-name", "Description.")
            //{
            //    optionOption,
            //    argumentArgument
            //};

            //commandCommand.Aliases.Add("execute");

            //rootCommand.Subcommands.Add(commandCommand);

            //commandCommand.SetAction(parseResult => NameFunction(parseResult.GetValue(argumentArgument)));

            // ///////////// //
            // REAL COMMANDS //
            // ///////////// //

            // Add cardinal
            Option<int> idOption = new("-id")
            {
                Description = "Specify id for new cardinal characteristic.",
                DefaultValueFactory = p => -1
            };
            idOption.Aliases.Add("--id");

            Option<bool> saveOption = new("--save")
            {
                Description = "Enable to save changes.",
                DefaultValueFactory = p => false
            };
            rootCommand.Options.Add(saveOption);

            Argument<string> nameArgument = new("name")
            {
                Description = "Name of cardinal characteristic."
                //DefaultValueFactory = parseResult => ""
            };

            Command createCCCommand = new("create", "Create a new cardinal characteristic.")
            {
                idOption,
                saveOption,
                nameArgument
            };

            createCCCommand.Aliases.Add("add");

            rootCommand.Subcommands.Add(createCCCommand);

            createCCCommand.SetAction(parseResult =>
            {
                var name = parseResult.GetValue(nameArgument);
                var newId = parseResult.GetValue(idOption);
                var save = parseResult.GetValue(saveOption);
                Console.WriteLine(newId);
                if (newId == -1)
                {
                    Env.AddCardinal(name);
                }
                else
                {
                    Env.AddCardinal(name, newId);
                }
                if (save)
                {
                    Env.Save();
                }
            });

            // Commands to add:
            // Load cardinals and relations
            // Add relations
            // Compute closure
            // Draw graph (with certain input numbers)
            // Save everything
            // Exit
            // Default command line behaviour that puts you in a command line style environment
            // Note that outside of the command line environment you'll want to specify which files you're loading and unloading each time...

            rootCommand.SetAction(parseResult => OldCommandInterface(Env, parseResult.Tokens.Select(t => t.Value).ToArray()));

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

        public static void OldCommandInterface(RelationEnvironment env, string[] args)
        {
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? "";
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
                        env.Save();
                        break;
                    case "create":
                        if (arguments.Length < 1)
                        {
                            Console.WriteLine("Usage: create <name>");
                            continue;
                        }
                        env.AddCardinal(string.Join(" ", arguments));
                        break;
                    case "relate":
                        if (arguments.Length != 2)
                        {
                            Console.WriteLine("Usage: create <id1> <id2>");
                            continue;
                        }
                        // TO DO !!
                        env.Relations.AddRelationByIds(int.Parse(arguments[0]), int.Parse(arguments[1]), '>');
                        Console.WriteLine($"Added relation {arguments[0]}>={arguments[1]}.");
                        break;
                    case "list":
                        foreach (CC c in env.Cardinals)
                        {
                            Console.WriteLine(c); // CCs have an override to their ToString()
                        }
                        break;
                    case "listrels":
                        foreach (Relation r in env.Relations.GetRelations())
                        {
                            Console.WriteLine($"{r.Item1} >= {r.Item2}.");
                        }
                        break;
                    case "plot":
                        var dot = GraphDrawer.DrawRelationDot(env.Relations.GetMinimalRelations(), env.Cardinals);
                        GraphDrawer.WriteDotFile(dot, Program.GetOutputPath(), "relations.dot", "graph.png");
                        break;
                    default:
                        Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
                        break;
                }
            }
        }

        public static string GetOutputPath(string filename)
        {
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"../../../"));
            return Path.Combine(projectRoot, "output", filename);
        }

        public static string GetOutputPath() => GetOutputPath("");

    }

    public class RelationEnvironment
    {
        private String baseDirectory { get; set; }
        public String Directory { get; set; }
        public String CCFile { get; set; } = "cardinal_characteristics";
        public String RelsFile { get; set; } = "relations";
        public String CCPath { get; set; }
        public String RelsPath { get; set; }
        private List<CC> LoadedCardinals;
        private HashSet<Relation> LoadedRelations;
        public RelationDatabase Relations = new RelationDatabase();
        public bool Unsaved { get; private set; } = false;
        public List<CC> Cardinals => Relations.Cardinals;


        public RelationEnvironment(string ccFile, string relsFile)
        {
            baseDirectory = AppContext.BaseDirectory;
            Directory = Path.GetFullPath(Path.Combine(baseDirectory, @"../../../assets/"));
            
            CCFile = ccFile;
            var ccExt = Path.GetExtension(ccFile);
            if (ccExt != ".json")
            {
                CCFile += ".json";
                if (ccExt != "")
                {
                    Console.WriteLine($"Warning: Cardinal characteristics file has extension other than .json. Programme will attempt to load {ccFile}.json");
                }
            }
            
            RelsFile = relsFile;
            var relsExt = Path.GetExtension(relsFile);
            if (relsExt != ".json")
            {
                RelsFile += ".json";
                if (relsExt != "")
                {
                    Console.WriteLine($"Warning: Cardinal characteristics file has extension other than .json. Programme will attempt to load {relsFile}.json");
                }
            }

            CCPath = Path.Combine(Directory, CCFile);
            RelsPath = Path.Combine(Directory, RelsFile);
            LoadedCardinals = JsonInterface.LoadCardinals(CCPath);
            LoadedRelations = JsonInterface.LoadRelations(RelsPath, LoadedCardinals);

            Relations = new RelationDatabase(LoadedCardinals, LoadedRelations);
        }

        public RelationEnvironment() : this("cardinal_characteristics", "relations")
        {
        }

        public void Save()
        {
            JsonInterface.SaveCardinals(CCPath, Relations.Cardinals);
            JsonInterface.SaveRelations(RelsPath, Relations.GetRelations());
            Unsaved = false;
        }

        public void AddCardinal(string name, int id)
        {
            Relations.AddCardinal(name, id);
            Unsaved = true;
        }
        public void AddCardinal(string name)
        {
            Relations.AddCardinal(name);
            Unsaved = true;
        }
    }
}