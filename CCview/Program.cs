// Actual feature list to implement pre-web-app:
// Modified graphs that show lines through based on a particular model
// Exporting to TikZ
// A basic collection of articles, models and cardinals to test with
// Better command line interface for plotting
// Commands to add custom articles, theorems and models
// Completely overhaul the Relation logic to work as being built off of AtomicRelations
// Use this to implement 'best (oldest) proof' style logic
// Clean up some of the excess variables lying around. I think that LoadedCardinals doesn't need to exist, for example
// Oh and make sure that we sort the List<T> things by item => item.Id, making sure to fill in blank spaces with nulls
// Or just turn it into a dictionary?
// OR just be more happy to FetchById
// Make JsonFileHandler.Load automatically create a dictionary and handle relations separately

// Dependencies
// These need to be cleaned up at some point

using System.CommandLine;
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CC = CCView.CardinalData.CardinalCharacteristic;
using CCView.JsonHandler;
using System.Collections;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCView
{
    public class Program
    {
        private static readonly bool _loadLog = true;
        public static bool ShouldExit { get; private set; } = false;
        static async Task<int> Main(string[] args)
        {
            //Console.WriteLine("Hi Callie! This is your reminder that we want to implement 'atomic' versus 'non-atomic' relations now. So make sure they can save/load properly and that the derivations work as intended. Throw in some checks to make sure that a derivation doesn't contain multiple of the same atomic relation, since that would allow infinite descending derivations of known theorems. Maybe also implement a GetAge() function.");
            //Console.WriteLine("Also get rid of ArtId in Relation.");

            // For now we assume that we are only ever working with one set of files: cardinal_characteristics.json and relations.json
            var Env = new RelationEnvironment();
            return await Run(Env, args);
        }

        public static async Task<int> Run(RelationEnvironment env, string[] args)
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
                DefaultValueFactory = p => [.. env.Cardinals.Keys]
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

            Option<string> citationOption = new("--cite")
            {
                Description = "The citation attached to a new article.",
                DefaultValueFactory = p => "No citation!"
            };

            Option<int> dateOption = new("--date")
            {
                Description = "Date in YYYYMMDD format. Use 99 for unknown values.",
                DefaultValueFactory = p => int.MaxValue
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

            Argument<int> idArgument = new("id")
            {
                Description = "Id of an object."
            };

            Argument<string> symbolArgument = new("symbol")
            {
                Description = "Symbol of the cardinal characteristic."
            };

            Argument<string[]> symbolsArgument = new("symbols")
            {
                Description = "Symbols of the cardinal characteristics."
            };

            Argument<int> zbArgument = new("ZBnumber")
            {
                Description = "Article number by the ZBMath organisational system (see zbmath.org). Do not include 'Zbl'."
            };

            // Commands //

            rootCommand.Options.Add(densityOption);
            rootCommand.Options.Add(fromShellOption);

            // Add cardinal
            Command createCommand = new("create", "Create a new specified object.");
            createCommand.Aliases.Add("add");

            Command createCC = new("cardinal", "Create a new cardinal characteristic.")
            {
                idOption,
                saveOption,
                nameArgument,
                symbolArgument
            };
            createCommand.Subcommands.Add(createCC);

            createCC.SetAction(parseResult =>
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
                nameArgument,
                idOption,
                dateOption,
                citationOption
            };
            createCommand.Subcommands.Add(createArticle);
            createArticle.SetAction(pR =>
            {
                string name = pR.GetValue(nameArgument) ?? "No name provided!";
                int id = pR.GetValue(idOption);
                int date = pR.GetValue(dateOption);
                string citation = pR.GetValue(citationOption) ?? "No citation provided!";
                if (id == -1)
                {
                    env.AddArticle(name, date, citation);
                }
                else
                {
                    env.AddArticle(name, date, citation, id);
                }
            });

            // Add relation
            Command createRelation = new("relation", "Add a relation between two cardinals.")
            {
                idsArgument,
                typeOption
            };
            createCommand.Subcommands.Add(createRelation);

            createRelation.SetAction(pR =>
            {
                int[] ids = pR.GetValue(idsArgument)!; // ! here is the null-forgiving operator, which is okay because we know that it is never null
                string typeString = pR.GetValue(typeOption) ?? "X";
                char type = typeString[0];
                env.RelateCardinals(ids[0], ids[1], type);
                if (Program._loadLog)
                {
                    var c1 = env.RelData.GetCardinalById(ids[0]);
                    var c2 = env.RelData.GetCardinalById(ids[1]);
                    Console.WriteLine($"Cardinals {c1} and {c2} related with type '{type}' relation.");
                }
            });

            // Add relation by symbol string
            Command relateSymbolCommand = new("relationbysymbol", "Add a relation between two cardinals by their symbols.")
            {
                symbolsArgument,
                typeOption
            };
            createCommand.Subcommands.Add(relateSymbolCommand);

            relateSymbolCommand.Aliases.Add("rbs");

            relateSymbolCommand.SetAction(pR =>
            {
                string[] symbols = pR.GetValue(symbolsArgument)!;
                string typeString = pR.GetValue(typeOption) ?? "X";
                char type = typeString[0];
                CC Item1 = env.RelData.GetCardinalBySymbol(symbols[0]);
                CC Item2 = env.RelData.GetCardinalBySymbol(symbols[1]);
                env.RelateCardinals(Item1.Id, Item2.Id, type);
                Program.LoadLog($"Cardinals {Item1} and {Item2} related with type '{type}' relation.");
            });

            // Add empty article
            Command createBlankArticle = new("barticle", "Add an empty article");
            createCommand.Subcommands.Add(createBlankArticle);

            createBlankArticle.SetAction(pR =>
            {
                env.AddArticle("Test", 99999999, "Test");
                Console.WriteLine("Added test article.");
            });

            // Add placeholder theorem
            Command createPlaceholderTheorem = new("ptheorem", "Add a placeholder theorem");
            createCommand.Subcommands.Add(createPlaceholderTheorem);

            createPlaceholderTheorem.SetAction(pR =>
            {
                int newId = RelationDatabase.NewDictId(env.Theorems);
                Theorem newTheorem = new(newId, env.Articles[0], [(env.RelData.GetCardinalBySymbol("D"), env.RelData.GetCardinalBySymbol("Item2Id"), '>'),
                    (env.RelData.GetCardinalBySymbol("Item2Id"), env.RelData.GetCardinalBySymbol("add(M)"), '>')], "Test");
                env.Theorems[newId] = newTheorem;
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

            // List objects
            Command listCommand = new("list", "List all objects of a particular type");

            Command listCC = new("list", "List all cardinal characteristics.");
            listCommand.Subcommands.Add(listCC);

            listCC.SetAction(pR =>
            {
                foreach (CC c in env.Cardinals.Values)
                {
                    Console.WriteLine(c);
                }
            });

            Command listRels = new("relations", "List of relations between cardinal characteristics.");
            listCommand.Subcommands.Add(listRels);

            listRels.SetAction(pR =>
            {
                foreach (Relation r in env.RelData.Relations)
                {
                    Console.WriteLine(r);
                }
            });

            Command listArts = new("articles", "List of articles in the database.");
            listCommand.Subcommands.Add(listArts);

            listArts.SetAction(pR =>
            {
                foreach (Article a in env.RelData.Articles.Values)
                {
                    Console.WriteLine(a);
                }
            });

            // Import article from ZBMath
            Command importArtCommand = new("import", "Import an article via a ZBMath number.")
            {
                zbArgument
            };
            rootCommand.Subcommands.Add(importArtCommand);

            importArtCommand.SetAction(async pR =>
            {
                int zb = pR.GetValue(zbArgument);
                await env.ImportArticle(zb);
             });

            Command importTestCommand = new("itest", "Test importing an article via a ZBMath number.");
            rootCommand.Subcommands.Add(importTestCommand);

            importTestCommand.SetAction(async pR =>
            {
                string zb = "7898322";
                await rootCommand.Parse(["--fromShell", "import", zb]).InvokeAsync();
            });

            // Print a citation
            Command citeCommand = new("cite", "Print an article citation")
            {
                idArgument
            };
            rootCommand.Subcommands.Add(citeCommand);

            citeCommand.SetAction(pR =>
            {
                int id = pR.GetValue(idArgument);
                Article? art = env.RelData.GetArticleById(id);
                if (art == null)
                {
                    Console.WriteLine($"No article of ID {id} exists.");
                }
                else
                {
                    Console.WriteLine(art.Citation);
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
                    shell.Run().GetAwaiter().GetResult();
                }
                else if (pR.GetValue(densityOption)) env.PopulateDensity();
            });

            return await rootCommand.Parse(args).InvokeAsync();
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
        private string BaseDirectory { get; set; }
        public string LoadDirectory { get; set; }
        // We should change from having a different '<T>File' and '<T>Path' etc to a few dictionaries
        //public Dictionary<string, string> LoadFiles { get; set; } = [];
        public string CCFile { get; set; } = "cardinal_characteristics";
        public string RelsFile { get; set; } = "relations";
        public string ArtsFile { get; set; } = "articles";
        public string ThmsFile { get; set; } = "theorems";
        public string ModsFile { get; set; } = "models";
        public string CCPath { get; set; }
        public string RelsPath { get; set; }
        public string ArtsPath { get; set; }
        public string ThmsPath { get; set; }
        public string ModsPath { get; set; }
        public string OutDirectory { get; set; }
        public string DotFile { get; set; } = "relations";
        public string GraphFile { get; set; } = "graph";
        public string DotPath { get; set; }
        public string GraphPath { get; set; }

        //private Dictionary<int, CC> LoadedCardinals = [];
        //private HashSet<Relation> LoadedRelations;
        //private Dictionary<int, Article> LoadedArticles;
        //private Dictionary<int, Theorem> LoadedTheorems;
        //private Dictionary<int, Model> LoadedModels;
        public RelationDatabase RelData = new RelationDatabase();
        public bool Unsaved { get; private set; } = false;
        public Dictionary<int, CC> Cardinals => RelData.Cardinals;
        public Dictionary<int, Article> Articles => RelData.Articles;
        public Dictionary<int, Theorem> Theorems => RelData.Theorems;
        public Dictionary<int, Model> Models => RelData.Models;
        public HashSet<Relation> Relations => RelData.Relations;


        public RelationEnvironment(string ccFile, string relsFile, string dotFile, string graphFile, string artsFile, string thmsFile, string modsFile)
        {
            Program.LoadLog("Loading Relation Environment.");
            BaseDirectory = AppContext.BaseDirectory;
            LoadDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../assets/"));
            OutDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../output/"));

            CCFile = AddExtension(ccFile, ".json", "Cardinal characteristics file");
            RelsFile = AddExtension(relsFile, ".json", "RelData file");
            DotFile = AddExtension(dotFile, ".dot", "Dot file");
            GraphFile = AddExtension(graphFile, ".png", "Graph file");
            ArtsFile = AddExtension(artsFile, ".json", "Articles file");
            ThmsFile = AddExtension(thmsFile, ".json", "Theorems file");
            ModsFile = AddExtension(modsFile, ".json", "Models file");

            CCPath = Path.Combine(LoadDirectory, CCFile);
            RelsPath = Path.Combine(LoadDirectory, RelsFile);
            DotPath = Path.Combine(OutDirectory, DotFile);
            GraphPath = Path.Combine(OutDirectory, GraphFile);
            ArtsPath = Path.Combine(LoadDirectory, ArtsFile);
            ThmsPath = Path.Combine(LoadDirectory, ThmsFile);
            ModsPath = Path.Combine(LoadDirectory, ModsFile);

            //LoadedCardinals = JsonInterface.LoadCardinals(CCPath);
            Program.LoadLog("Loading cardinals.");
            var LoadedCardinals = JsonFileHandler.Load<CC>(CCPath);
            Program.LoadLog("Loading articles.");
            var LoadedArticles = JsonFileHandler.Load<Article>(ArtsPath);
            Program.LoadLog("Loading theorems.");
            // LoadedCardinals.Values.ToList() is a bad solution for the moment
            var LoadedTheorems = JsonFileHandler.LoadTheorems(ThmsPath, LoadedCardinals, LoadedArticles);
            Program.LoadLog("Loading models.");
            var LoadedModels = JsonFileHandler.LoadModels(ModsPath, LoadedCardinals, LoadedArticles, LoadedTheorems);
            Program.LoadLog("Loading relations.");
            var LoadedRelations = JsonFileHandler.LoadRelations(RelsPath, LoadedCardinals, LoadedTheorems, LoadedModels);
            Program.LoadLog("Creating Relation Database environment.");
            RelData = new RelationDatabase(LoadedCardinals, LoadedRelations, LoadedArticles, LoadedTheorems, LoadedModels);
            Program.LoadLog("Populating atomic relations.");
            RelData.GenerateAtoms();
            RelData.CreateTrivialRelations();
            Program.LoadLog("Relation Environment complete.");
        }

        public RelationEnvironment() : this("cardinal_characteristics", "relations", "relations", "graph", "articles", "theorems", "models")
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
            JsonFileHandler.Save(CCPath, Cardinals.Values);
            Program.LoadLog($"Cardinals saved to {CCPath}.");
            JsonFileHandler.Save(RelsPath, RelData.Relations.ToList());
            Program.LoadLog($"RelData saved to {RelsPath}.");
            JsonFileHandler.Save(ArtsPath, Articles.Values);
            Program.LoadLog($"Articles saved to {ArtsPath}.");
            JsonFileHandler.Save(ThmsPath, Theorems.Values);
            Program.LoadLog($"Theorems saved to {ThmsPath}.");
            JsonFileHandler.Save(ModsPath, Models.Values);
            Program.LoadLog($"Models saved to {ModsPath}.");
        }

        public void AddCardinal(string? name, string? symbol, int id)
        {
            RelData.AddCardinal(name, symbol, id);
            Unsaved = true;
        }
        public void AddCardinal(string? name, string? symbol)
        {
            RelData.AddCardinal(name, symbol);
            Unsaved = true;
        }
        public void RelateCardinals(int idOne, int idTwo, char type)
        {
            RelData.AddRelationByIds(idOne, idTwo, type);
            Unsaved = true;
        }
        public void AddArticle(string name, int date, string citation, int id)
        {
            RelData.AddArticle(name, date, citation, id);
            Unsaved = true;
        }
        public void AddArticle(string name, int date, string citation)
        {
            RelData.AddArticle(name, date, citation);
            Unsaved = true;
        }
        public async Task ImportArticle(int zb)
        {
            string url = $"https://api.zbmath.org/v1/document/{zb}";
            Console.WriteLine($"Attempting to access {url}.");
            string json = await InteractiveShell.GETString(url);
            Article article = JsonFileHandler.DeserializeArticle(json);
            RelData.AddArticle(article);
            Console.WriteLine($"New article {article} added.");
            Unsaved = true;
        }
        public void TransClose()
        {
            int numNewRels = RelData.TransClose();
            if (numNewRels > 0) Unsaved = true;
        }

        public string PlotGraphDot(int[] ids, string fileName)
        {
            List<CC> cardinals = [.. ids.Select(id => RelData.Cardinals[id])];
            var dot = GraphLogic.Vis.GraphDrawer.GenerateGraph(cardinals, RelData.GetMinimalRelations(cardinals));
            GraphLogic.Vis.GraphDrawer.WriteDotFile(dot, Path.Combine(OutDirectory, fileName));
            return dot;
        }

        public string PlotGraphDot(int[] ids)
        {
            return PlotGraphDot(ids, DotFile);
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
            if (RelData.PopulateDensity()) Unsaved = true;
        }
    }
    public class InteractiveShell
    {
        private bool ShouldExit = false;
        private readonly RelationEnvironment env;
        private readonly RootCommand rootCommand;
        private static readonly HttpClient Client = new();
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; 
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
        public async Task Run()
        {
            while (!ShouldExit)
            {
                Console.Write("> ");
                string input = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(input)) continue;
                string[] args = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                args = [.. args.Prepend("--fromShell")];
                await rootCommand.Parse(args).InvokeAsync();
            }
        }
        public static async Task<string> GETString(string url)
        {
            try
            {
                return await Client.GetStringAsync(url);
            }
            catch (HttpRequestException e)
            {
                throw new ArgumentException($"Error fetching page: {e.Message}");
            }
        }
    }
}