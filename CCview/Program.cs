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

            return Run(Env, args);
        }

        public static int Run(RelationEnvironment env, string[] args)
        {
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

            // Options //
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

            Option<char> typeOption = new("--type")
            {
                Description = "Type of relation.",
                DefaultValueFactory = p => '>'
            };

            Option<bool> pngOption = new("--toPng")
            {
                Description = "Also saves graph as a png.",
                DefaultValueFactory = p > false
            };

            Option<string> fileOption = new("--saveAs")
            {
                Description = "Save as a specified file."
            };

            // Arguments //
            Argument<string> nameArgument = new("name")
            {
                Description = "Name of cardinal characteristic."
                //DefaultValueFactory = parseResult => ""
            };

            Argument<int[]> idArgument = new("ids")
            {
                Description = "Ids of cardinal characteristics."
            };

            // Commands //

            // Add cardinal
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
                string? name = parseResult.GetValue(nameArgument);
                var newId = parseResult.GetValue(idOption);
                var save = parseResult.GetValue(saveOption);
                if (newId == -1)
                {
                    env.AddCardinal(name);
                }
                else
                {
                    env.AddCardinal(name, newId);
                }
                if (save)
                {
                    env.Save();
                }
            });

            // Add relation
            Command relateCommand = new("relate", "Add a relation between two cardinals.")
            {
                idArgument,
                typeOption
            };
            rootCommand.Subcommands.Add(relateCommand);

            relateCommand.SetAction(pR =>
            {
                int[] ids = pR.GetValue(idArgument);
                char type = pR.GetValue(typeOption);
                env.RelateCardinals(ids[0], ids[1], type);
            });

            // Save
            Command saveCommand = new("save", "Save the cardinals and relations.");
            rootCommand.Subcommands.Add(saveCommand);

            saveCommand.SetAction(pR => env.Save());

            //Plot
            Command plotCommand = new("plot", "Draw the relations as a dot graph.")
            {
                pngOption,
                fileOption
            };
            rootCommand.Subcommands.Add(plotCommand);

            plotCommand.SetAction(pR =>
            {
                env.PlotGraphDot([]);
                // TO DO !!
            });


            // Commands to add:
            // Load cardinals and relations
            // Compute closure
            // Draw graph (with certain input numbers)
            // Save everything
            // Exit
            // Default command line behaviour that puts you in a command line style environment
            // Note that outside of the command line environment you'll want to specify which files you're loading and unloading each time...

            // ccv
            Command commandLineCommand = new("ccv", "Old command line behaviour.");
            rootCommand.Subcommands.Add(commandLineCommand);
            commandLineCommand.SetAction(pR =>
            {
                OldCommandInterface(env, pR.Tokens.Select(t => t.Value).ToArray());
            });

            //ccn
            Command newLineCommand = new("ccn", "New command line behaviour.");
            rootCommand.Subcommands.Add(newLineCommand);
            newLineCommand.SetAction(pR =>
            {
                while (true)
                {
                    Console.Write("> ");
                    string input = Console.ReadLine() ?? "";
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }
                    string[] args = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (args[0].Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (env.Unsaved)
                        {
                            Console.Write("You have unsaved changes. Are you sure? [Y]es/[N]o (Default No). ");
                            string verify = Console.ReadLine() ?? "N";
                            if (verify.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || verify.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                    rootCommand.Parse(args).Invoke();
                }
            });

            rootCommand.SetAction(pR => Console.WriteLine("Default behaviour!"));

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

        public void AddCardinal(string? name, int id)
        {
            Relations.AddCardinal(name, id);
            Unsaved = true;
        }
        public void AddCardinal(string? name)
        {
            Relations.AddCardinal(name);
            Unsaved = true;
        }
        public void RelateCardinals(int idOne, int idTwo, char type)
        {
            Relations.AddRelationByIds(idOne, idTwo, type);
            Unsaved = true;
        }

        public void PlotGraphDot(int[] ids) // IMPLEMENT
        {
            return;
        }
    }
}