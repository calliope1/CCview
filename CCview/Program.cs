// Actual feature list to implement pre-web-app:
// Modified graphs that show lines through based on a particular model
// Exporting to TikZ
// Implementing the 'a cannot be proved to be >= b' relation (how does this affect use?)
// Implement an Article and Model hash list like with Cardinals
// Logic to tell than 'a \ngeqVdash b' (with earliest possible signature)
// A basic collection of articles, models and cardinals to test with
// Better command line interface for plotting
// Change (Relations, Cardinals) to (Cardinals, Relations) in functions
// MAYBE Change Relations to a List rather than a HashSet

// Dependencies
// These need to be cleaned up at some point

using System.CommandLine;
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CC = CCView.CardinalData.CardinalCharacteristic;
using CCView.JsonHandler;

namespace CCView
{
    public class Program
    {
        public static bool _loadLog { get; private set;  } = true;
        public static bool ShouldExit { get; private set; } = false;
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
            Option<int> idOption = new("--id")
            {
                Description = "Specify id for new cardinal characteristic.",
                DefaultValueFactory = p => -1
            };

            Option<int[]> idsOption = new("--ids")
            {
                Description = "Specify multiple ids for cardinal characteristics.",
                DefaultValueFactory = p => env.GetIdList()
            };

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
                DefaultValueFactory = p => false
            };

            Option<string> fileOption = new("--saveAs")
            {
                Description = "Save as a specified file.",
                DefaultValueFactory = p => null! // Null-forgiving operator
            };

            // Arguments //
            Argument<string> nameArgument = new("name")
            {
                Description = "Name of cardinal characteristic."
                //DefaultValueFactory = parseResult => ""
            };

            Argument<int[]> idsArgument = new("ids")
            {
                Description = "Ids of cardinal characteristics."
            };

            Argument<string> symbolArgument = new("symbol")
            {
                Description = "Symbol of the cardinal characteristic."
            };

            Argument<string[]> symbolsArgument = new("symbols")
            {
                Description = "Symbols of the cardinal characteristics."
            };

            // Commands //

            // Add cardinal
            Command createCCCommand = new("create", "Create a new cardinal characteristic.")
            {
                idOption,
                saveOption,
                nameArgument,
                symbolArgument
            };
            createCCCommand.Aliases.Add("add");
            rootCommand.Subcommands.Add(createCCCommand);

            createCCCommand.SetAction(parseResult =>
            {
                string? name = parseResult.GetValue(nameArgument);
                string? symbol = parseResult.GetValue(symbolArgument);
                var newId = parseResult.GetValue(idOption);
                var save = parseResult.GetValue(saveOption);
                if (newId == -1)
                {
                    env.AddCardinal(name, symbol);
                }
                else
                {
                    env.AddCardinal(name, symbol, newId);
                }
                if (save)
                {
                    env.Save();
                }
            });

            // Add relation
            Command relateCommand = new("relate", "Add a relation between two cardinals.")
            {
                idsArgument,
                typeOption
            };
            rootCommand.Subcommands.Add(relateCommand);

            relateCommand.SetAction(pR =>
            {
                int[] ids = pR.GetValue(idsArgument)!; // ! here is the null-forgiving operator, which is okay because we know that it is never null
                char type = pR.GetValue(typeOption);
                env.RelateCardinals(ids[0], ids[1], type);
                if (Program._loadLog)
                {
                    var c1 = env.Relations.GetCardinalById(ids[0]);
                    var c2 = env.Relations.GetCardinalById(ids[1]);
                    Console.WriteLine($"Cardinals {c1} and {c2} related with type '>' relation.");
                }
            });

            // Add relation by symbol string
            Command relateSymbolCommand = new("relateSymbol", "Add a relation between two cardinals by their symbols.")
            {
                symbolsArgument,
                typeOption
            };
            rootCommand.Subcommands.Add(relateSymbolCommand);

            relateSymbolCommand.Aliases.Add("rs");

            relateSymbolCommand.SetAction(pR =>
            {
                string[] symbols = pR.GetValue(symbolsArgument)!;
                char type = pR.GetValue(typeOption);
                CC Item1 = env.Relations.GetCardinalBySymbol(symbols[0]);
                CC Item2 = env.Relations.GetCardinalBySymbol(symbols[1]);
                env.RelateCardinals(Item1.Id, Item2.Id, type);
                if (Program._loadLog)
                {
                    Console.WriteLine($"Cardinals {Item1} and {Item2} related with type '>' relation.");
                }
            });

            // Compute transitive closure
            Command transCommand = new("trans", "Compute transitive closure of relations.");
            rootCommand.Subcommands.Add(transCommand);

            transCommand.SetAction(pR =>
            {
                env.Relations.TransClose();
            });

            // Save
            Command saveCommand = new("save", "Save the cardinals and relations.");
            rootCommand.Subcommands.Add(saveCommand);
            saveCommand.SetAction(pR => env.Save());

            //Plot
            Command plotCommand = new("plot", "Draw the relations as a dot graph.") // IDs doesn't really work properly here
            {
                pngOption,
                fileOption,
                idsOption
            };
            rootCommand.Subcommands.Add(plotCommand);

            plotCommand.SetAction(pR =>
            {
                bool png = pR.GetValue(pngOption);
                string file = pR.GetValue(fileOption)!;
                int[] ids = pR.GetValue(idsOption)!;
                if (file != null)
                {
                    env.PlotGraphDot(ids, file);
                    if (png) env.PlotGraphPng(RelationEnvironment.AddExtension(file, ".png", "Graph save file"), file);
                }
                else
                {
                    env.PlotGraphDot(ids, env.DotFile);
                    if (png) env.PlotGraphPng(env.DotFile, env.GraphFile);
                }
            });

            // List cardinals
            Command listCommand = new("list", "List all cardinal characteristics.");
            rootCommand.Subcommands.Add(listCommand);

            listCommand.SetAction(pR =>
            {
                foreach (CC c in env.Cardinals)
                {
                    Console.WriteLine(c);
                }
            });

            // Commands to add:
            // Load cardinals and relations
            // Compute closure
            // Note that outside of the command line environment you'll want to specify which files you're loading and unloading each time...

            // root command puts us into a command line shell
            rootCommand.SetAction(pR =>
            {
                if (Program._loadLog)
                {
                    Console.WriteLine("Creating Interactive Shell.");
                }
                var shell = new InteractiveShell(env, rootCommand);
                if (Program._loadLog)
                {
                    Console.WriteLine("Interactive Shell created.");
                }
                if (Program._loadLog)
                {
                    Console.WriteLine("Loading complete.");
                }
                shell.Run();
            });

            return rootCommand.Parse(args).Invoke();
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
        private String BaseDirectory { get; set; }
        public String LoadDirectory { get; set; }
        public String CCFile { get; set; } = "cardinal_characteristics";
        public String RelsFile { get; set; } = "relations";
        public String CCPath { get; set; }
        public String RelsPath { get; set; }
        public String OutDirectory { get; set; }
        public String DotFile { get; set; } = "relations";
        public String GraphFile { get; set; } = "graph";
        public String DotPath { get; set; }
        public String GraphPath { get; set; }

        private List<CC> LoadedCardinals;
        private HashSet<Relation> LoadedRelations;
        public RelationDatabase Relations = new RelationDatabase();
        public bool Unsaved { get; private set; } = false;
        public List<CC> Cardinals => Relations.Cardinals;


        public RelationEnvironment(string ccFile, string relsFile, string dotFile, string graphFile)
        {
            if (Program._loadLog)
            {
                Console.WriteLine("Loading Relation Environment.");
            }
            BaseDirectory = AppContext.BaseDirectory;
            LoadDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../assets/"));
            OutDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../output/"));

            CCFile = AddExtension(ccFile, ".json", "Cardinal characteristics file");
            RelsFile = AddExtension(relsFile, ".json", "Relations file");
            DotFile = AddExtension(dotFile, ".dot", "Dot file");
            GraphFile = AddExtension(graphFile, ".png", "Graph file");

            CCPath = Path.Combine(LoadDirectory, CCFile);
            RelsPath = Path.Combine(LoadDirectory, RelsFile);
            DotPath = Path.Combine(OutDirectory, DotFile);
            GraphPath = Path.Combine(OutDirectory, GraphFile);

            //LoadedCardinals = JsonInterface.LoadCardinals(CCPath);
            if (Program._loadLog)
            {
                Console.WriteLine("Loading cardinals.");
            }
            LoadedCardinals = JsonFileHandler.Load<CC>(CCPath);
            if (Program._loadLog)
            {
                Console.WriteLine("Loading relations.");
            }
            LoadedRelations = JsonFileHandler.LoadRelations(RelsPath, LoadedCardinals).ToHashSet();
            if (Program._loadLog)
            {
                Console.WriteLine("Creating Relation Database environment.");
            }
            Relations = new RelationDatabase(LoadedCardinals, LoadedRelations);
            if (Program._loadLog)
            {
                Console.WriteLine("Relation Environment complete.");
            }
        }

        public RelationEnvironment() : this("cardinal_characteristics", "relations", "relations", "graph")
        {
        }

        public static string AddExtension(string file, string ext, string fileDescription)
            // Checks if a file name has an appropriate extension. Warns if not.
        {
            var outFile = file;
            var fExt = Path.GetExtension(file);
            if (fExt != ext)
            {
                outFile += ext;
                if (fExt != "")
                {
                    Console.WriteLine($"Warning: {fileDescription} has extension other than ${ext}. Programme will attempt to load/save with {file}.json");
                }
            }
            return outFile;
        }

        public static string AddExtension(string file, string ext)
        {
            return AddExtension(file, ext, "File");
        }


        public void Save()
        {
            Unsaved = false;
            JsonFileHandler.Save<CC>(CCPath, Cardinals);
            if (Program._loadLog)
            {
                Console.WriteLine($"Cardinals saved to {CCPath}.");
            }
            JsonFileHandler.Save<Relation>(RelsPath, Relations.Relations.ToList());
            if (Program._loadLog)
            {
                Console.WriteLine($"Relations saved to {RelsPath}.");
            }
        }

        public void AddCardinal(string? name, string? symbol, int id)
        {
            Relations.AddCardinal(name, symbol, id);
            Unsaved = true;
        }
        public void AddCardinal(string? name, string? symbol)
        {
            Relations.AddCardinal(name, symbol);
            Unsaved = true;
        }
        public void RelateCardinals(int idOne, int idTwo, char type)
        {
            Relations.AddRelationByIds(idOne, idTwo, type);
            Unsaved = true;
        }

        public string PlotGraphDot(int[] ids, string fileName)
        {
            List<CC> cardinals = ids.Select(id => Relations.GetCardinalByIdOrThrow(id)).ToList();
            var dot = GraphLogic.Vis.GraphDrawer.GenerateGraph(Relations.GetMinimalRelations(cardinals), cardinals);
            GraphLogic.Vis.GraphDrawer.WriteDotFile(dot, Path.Combine(OutDirectory, fileName));
            return dot;
        }

        public string PlotGraphDot(int[] ids)
        {
            return PlotGraphDot(ids, DotFile);
        }
        public int[] GetIdList()
        {
            return Cardinals.Select(c => c.Id).ToArray();
        }
        public void PlotGraphPng(string dotFileName, string pngFileName)
        {
            GraphLogic.Vis.GraphDrawer.WritePngFile(OutDirectory, dotFileName, OutDirectory, pngFileName);
        }
        public void PlotGraphPng(string dot)
        {
            PlotGraphPng(DotFile, GraphFile);
        }
    }
    public class InteractiveShell
    {
        private bool ShouldExit = false;
        private readonly RelationEnvironment env;
        private readonly RootCommand rootCommand;
        public InteractiveShell(RelationEnvironment env, RootCommand rootCommand)
        {
            this.env = env;
            this.rootCommand = rootCommand;
            AddExitCommand();
        }
        public void AddExitCommand()
        {
            var exitCommand = new Command("exit", "Exit the shell.");
            exitCommand.SetAction(pR =>
            {
                if (env.Unsaved)
                {
                    Console.Write("You have unsaved changes. Exit anyway? [Y]es/[N]o (Default No): ");
                    string verify = Console.ReadLine() ?? "N";
                    if (verify.Equals("yes", StringComparison.OrdinalIgnoreCase) || verify.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        ShouldExit = true;
                    }
                }
                else
                {
                    ShouldExit = true;
                }
                return Task.CompletedTask;
            });

            rootCommand.Subcommands.Add(exitCommand);
        }
        public void Run()
        {
            while (!ShouldExit)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(input)) continue;

                string[] args = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                rootCommand.Parse(args).Invoke();
            }
        }
    }
}