// Actual feature list to implement pre-web-app:
// Modified graphs that show lines through based on a particular model
// Exporting to TikZ
// Implementing the 'a cannot be proved to be >= b' relation (how does this affect use?)
// Implement an Article and Model hash list like with Cardinals
// Logic to tell than 'a \ngeqVdash b' (with earliest possible signature)
// A basic collection of articles, models and cardinals to test with
// Better command line interface for plotting
// Change (Relations, Cardinals) to (Cardinals, Relations) in functions

// Dependencies
// These need to be cleaned up at some point

using System.CommandLine;
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CC = CCView.CardinalData.CardinalCharacteristic;
using CCView.JsonHandler;
using System.Collections;

namespace CCView
{
    public class Program
    {
        private static readonly bool _loadLog = true;
        public static bool ShouldExit { get; private set; } = false;
        static int Main(string[] args)
        {
            //Console.WriteLine("Hi Callie! This is your reminder that we want to implement 'atomic' versus 'non-atomic' relations now. So make sure they can save/load properly and that the derivations work as intended. Throw in some checks to make sure that a derivation doesn't contain multiple of the same atomic relation, since that would allow infinite descending derivations of known theorems. Maybe also implement a GetAge() function.");
            //Console.WriteLine("Also get rid of ArtId in Relation.");

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
            Option<bool> fromShellOption = new("--fromShell")
            {
                Description = "Internal-only option to indicate to the programme that commands are being called from the internal shell.",
                DefaultValueFactory = p => false,
                Hidden = true
            };

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

            Option<string> typeOption = new("--type")
            {
                Description = "Type of relation.",
                DefaultValueFactory = p => ">"
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

            Option<bool> densityOption = new("--btwn")
            {
                Description = "Compute the in-betweenness relation. (Slower initially, but makes constructing subsequent graphs faster).",
                DefaultValueFactory = p => false
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

            rootCommand.Options.Add(densityOption);
            rootCommand.Options.Add(fromShellOption);

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

            // Add article
            Command createArticle = new("article", "Create an article.")
            {
                idOption,
                //dateOption,
                //citationOption,
                nameArgument
            };
            createCCCommand.Subcommands.Add(createArticle);
            createArticle.SetAction(pR =>
            {
                int id = pR.GetValue(idOption);
                string name = pR.GetValue(nameArgument) ?? "No name provided!";
                Article newArt = new(id, int.MaxValue, name, "Citations not yet implemented!");
                // This is NOT how we should be doing it! But I need to do some work and this is just to test the saving features
                env.Relations.Articles.Add(newArt);
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
                string typeString = pR.GetValue(typeOption) ?? "X";
                char type = typeString[0];
                env.RelateCardinals(ids[0], ids[1], type);
                if (Program._loadLog)
                {
                    var c1 = env.Relations.GetCardinalById(ids[0]);
                    var c2 = env.Relations.GetCardinalById(ids[1]);
                    Console.WriteLine($"Cardinals {c1} and {c2} related with type '{type}' relation.");
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
                string typeString = pR.GetValue(typeOption) ?? "X";
                char type = typeString[0];
                CC Item1 = env.Relations.GetCardinalBySymbol(symbols[0]);
                CC Item2 = env.Relations.GetCardinalBySymbol(symbols[1]);
                env.RelateCardinals(Item1.Id, Item2.Id, type);
                Program.LoadLog($"Cardinals {Item1} and {Item2} related with type '{type}' relation.");
            });

            // Compute transitive closure
            Command transCommand = new("trans", "Compute transitive closure of relations.");
            rootCommand.Subcommands.Add(transCommand);

            transCommand.SetAction(pR =>
            {
                env.TransClose();
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

            Command listRelsCommand = new("relations", "List of relations between cardinal characteristics.");
            listCommand.Subcommands.Add(listRelsCommand);

            listRelsCommand.SetAction(pR =>
            {
                foreach (Relation r in env.Relations.Relations)
                {
                    Console.WriteLine(r);
                }
            });

            // root command puts us into a command line shell
            rootCommand.SetAction(pR =>
            {
                if (!pR.GetValue(fromShellOption))
                {
                    Program.LoadLog("Creating Interactive Shell.");
                    var shell = new InteractiveShell(env, rootCommand);
                    Program.LoadLog("Interactive Shell created.");
                    if (pR.GetValue(densityOption)) env.PopulateDensity();
                    Program.LoadLog("Loading complete.");
                    shell.Run();
                }
                else if (pR.GetValue(densityOption)) env.PopulateDensity();
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
        public static string LoadLog(string message)
        {
            if (_loadLog) Console.WriteLine(message);
            return _loadLog ? message : "";
        }
    }

    public class RelationEnvironment
    {
        private String BaseDirectory { get; set; }
        public String LoadDirectory { get; set; }
        public String CCFile { get; set; } = "cardinal_characteristics";
        public String RelsFile { get; set; } = "relations";
        public string ArtsFile { get; set; } = "articles";
        public String CCPath { get; set; }
        public String RelsPath { get; set; }
        public string ArtsPath { get; set; }
        public String OutDirectory { get; set; }
        public String DotFile { get; set; } = "relations";
        public String GraphFile { get; set; } = "graph";
        public String DotPath { get; set; }
        public String GraphPath { get; set; }

        private List<CC> LoadedCardinals;
        private HashSet<Relation> LoadedRelations;
        private List<Article> LoadedArticles;
        public RelationDatabase Relations = new RelationDatabase();
        public bool Unsaved { get; private set; } = false;
        public List<CC> Cardinals => Relations.Cardinals;
        public List<Article> Articles => Relations.Articles;


        public RelationEnvironment(string ccFile, string relsFile, string dotFile, string graphFile, string artsFile)
        {
            Program.LoadLog("Loading Relation Environment.");
            BaseDirectory = AppContext.BaseDirectory;
            LoadDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../assets/"));
            OutDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../output/"));

            CCFile = AddExtension(ccFile, ".json", "Cardinal characteristics file");
            RelsFile = AddExtension(relsFile, ".json", "Relations file");
            DotFile = AddExtension(dotFile, ".dot", "Dot file");
            GraphFile = AddExtension(graphFile, ".png", "Graph file");
            ArtsFile = AddExtension(artsFile, ".json", "Articles file");

            CCPath = Path.Combine(LoadDirectory, CCFile);
            RelsPath = Path.Combine(LoadDirectory, RelsFile);
            DotPath = Path.Combine(OutDirectory, DotFile);
            GraphPath = Path.Combine(OutDirectory, GraphFile);
            ArtsPath = Path.Combine(LoadDirectory, ArtsFile);

            //LoadedCardinals = JsonInterface.LoadCardinals(CCPath);
            Program.LoadLog("Loading cardinals.");
            LoadedCardinals = JsonFileHandler.Load<CC>(CCPath);
            Program.LoadLog("Loading relations.");
            LoadedRelations = JsonFileHandler.LoadRelations(RelsPath, LoadedCardinals).ToHashSet();
            Program.LoadLog("Not loading articles because article loading is un-implemented.");
            LoadedArticles = [];
            Program.LoadLog("Creating Relation Database environment.");
            Relations = new RelationDatabase(LoadedCardinals, LoadedRelations);
            Program.LoadLog("Relation Environment complete.");
        }

        public RelationEnvironment() : this("cardinal_characteristics", "relations", "relations", "graph", "articles")
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
            JsonFileHandler.Save(CCPath, Cardinals);
            Program.LoadLog($"Cardinals saved to {CCPath}.");
            JsonFileHandler.Save(RelsPath, Relations.Relations.ToList());
            Program.LoadLog($"Relations saved to {RelsPath}.");
            JsonFileHandler.Save(ArtsPath, Articles);
            Program.LoadLog($"Articles saved to {ArtsPath}.");
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
        public void TransClose()
        {
            int numNewRels = Relations.TransClose();
            if (numNewRels > 0) Unsaved = true;
        }

        public string PlotGraphDot(int[] ids, string fileName)
        {
            List<CC> cardinals = ids.Select(id => Relations.GetCardinalByIdOrThrow(id)).ToList();
            var dot = GraphLogic.Vis.GraphDrawer.GenerateGraph(cardinals, Relations.GetMinimalRelations(cardinals));
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
        public void PopulateDensity()
        {
            if (Relations.PopulateDensity()) Unsaved = true;
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
                args = [.. args.Prepend("--fromShell")];
                rootCommand.Parse(args).Invoke();
            }
        }
    }
}