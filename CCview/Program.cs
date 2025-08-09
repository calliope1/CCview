// Actual feature list to implement pre-web-app:
// Modified graphs that show lines through based on a particular model
// Exporting to TikZ
// A basic collection of articles, models and cardinals to test with
// Better command line interface for plotting
// Commands to add custom articles, theorems and models
// Implement 'best (oldest) proof' style logic (done?)
// Add validators to the arguments and options

// Dependencies
// These need to be cleaned up at some point
using CCView.CardinalData;
using CCView.CardinalData.Compute;
using CCView.GraphLogic;
using CCView.JsonHandler;
using System;
using System.Collections;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;
using CC = CCView.CardinalData.CardinalCharacteristic;

namespace CCView
{
    public class Program
    {
        private static readonly bool _loadLog = true;
        public static bool ShouldExit { get; private set; } = false;
        static async Task<int> Main(string[] args)
        {
            // For now we assume that we are only ever working with one set of files: cardinal_characteristics.json and relations.json
            var Env = new RelationEnvironment();
            return await Run(Env, args);
        }

        public static async Task<int> Run(RelationEnvironment env, string[] args)
        {
            RootCommand rootCommand = new("Cardinal characteristics visualiser!");
            Command hiddenCommand = new("hidden", "Internal-only commands that cannot be called by the user.");

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
                DefaultValueFactory = p => null!
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

            Option<bool> noSymbolsOption = new("--noSymbols")
            {
                Description = "Invoke to not include symbols in the description of cardinals (only id numbers)",
                DefaultValueFactory = p => false
            };

            Option<string[]> containsCardinalsOption = new("--cardinals")
            {
                Description = "Restrict search to those pertaining to the described cardinals",
                DefaultValueFactory = p => []
            };
            containsCardinalsOption.Aliases.Add("-c");

            Option<string[]> containsModelsOption = new("--models")
            {
                Description = "Restrict search to those pertaining to the described models",
                DefaultValueFactory = p => []
            };
            containsModelsOption.Aliases.Add("-m");

            Option<string[]> containsTypesOption = new("--types")
            {
                Description = "Restrict search to those pertaining to the given types",
                DefaultValueFactory = p => []
            };
            containsModelsOption.Aliases.Add("-ty");

            Option<string[]> inArticlesOption = new("--articles")
            {
                Description = "Restrict search to those pertaining to the described articles",
                DefaultValueFactory = p => []
            };
            containsModelsOption.Aliases.Add("-ar");

            Option<string[]> fromTheoremsOption = new("--theorems")
            {
                Description = "Restrict search to those pertaining to the described theorems",
                DefaultValueFactory = p => []
            };
            containsModelsOption.Aliases.Add("-th");

            Option<string[]> fromAgeSetOption = new("--ages")
            {
                Description = "Restrict search to those pertaining to the described ages",
                DefaultValueFactory = p => []
            };
            containsModelsOption.Aliases.Add("-ag");

            Option<string[]> fromIdSetOption = new("--ids")
            {
                Description = "Restrict search to those with the described ids",
                DefaultValueFactory = p => []
            };
            containsModelsOption.Aliases.Add("-i");

            // Arguments //
            Argument<string> nameArgument = new("name")
            {
                Description = "Name of cardinal characteristic."
            };

            Argument<int[]> idsArgument = new("ids")
            {
                Description = "Ids of cardinal characteristics."
            };

            Argument<int> idArgument = new("id")
            {
                Description = "Id of an object."
            };

            Argument<int> idArgumentTwo = new("id")
            {
                Description = "Id of a second object"
            };

            Argument<string> symbolArgument = new("symbol")
            {
                Description = "Symbol of the cardinal characteristic."
            };

            Argument<string[]> symbolsArgument = new("symbols")
            {
                Description = "Symbols of the cardinal characteristics."
            };

            Argument<string> zbArgument = new("ZBnumber")
            {
                Description = "Article number by the ZBMath organisational system (see zbmath.org). Do not include 'Zbl'."
            };

            Argument<int> witnessIdArgument = new("Witness ID")
            {
                Description = "ID for the theorem that witnesses the given result."
            };

            Argument<int> articleIdArgument = new("Article ID")
            {
                Description = "id of an article.",
                DefaultValueFactory = p => -1
            };

            Argument<string> descriptionArgument = new("Description")
            {
                Description = "Description of the new object.",
                DefaultValueFactory = p => "No description provided"
            };

            Argument<string> fileArgument = new("File")
            {
                Description = "Path to a file from assets."
            };

            Argument<string> typeArgument = new("Type")
            {
                Description = "Type of the relation",
                DefaultValueFactory = p => "X"
            };

            Argument<string> dotFiletypeArgument = new("dot")
            {
                Description = "dot process filetype argument (e.g. --Tpng)",
                DefaultValueFactory = p => "",
                Arity = ArgumentArity.ZeroOrOne
            };

            Argument<string[]> descriptionOfObjectsArgument = new("description of objects")
            {
                Description = "Array of ints or strings representing the ids or symbols of objects",
                DefaultValueFactory = p => []
            };

            // Commands //

            rootCommand.Options.Add(densityOption);
            rootCommand.Options.Add(fromShellOption);

            // List objects
            Command listCommand = new("list", "List all objects of a particular type");
            rootCommand.Subcommands.Add(listCommand);

            Command listCC = new("cardinals", "List all cardinal characteristics.");
            listCommand.Subcommands.Add(listCC);

            listCC.SetAction(pR =>
            {
                foreach (CC c in env.Cardinals.Values)
                {
                    Console.WriteLine(c);
                }
            });

            Command listRels = new("relations", "List of relations between cardinal characteristics.")
            {
                noSymbolsOption
            };
            listCommand.Subcommands.Add(listRels);

            listRels.SetAction(pR =>
            {
                if (!pR.GetValue(noSymbolsOption))
                {
                    foreach (Relation r in env.RelData.Relations.Values)
                    {
                        Console.WriteLine(r.ToStringWithSymbols(env.RelData.Cardinals));
                    }
                }
                else
                {
                    foreach (Relation r in env.RelData.Relations.Values)
                    {
                        Console.WriteLine(r);
                    }
                }
                return 0;
            });

            Command listRelation = new("relation", "Get more information about a given relation.")
            {
                idArgument
            };
            listCommand.Subcommands.Add(listRelation);

            listRelation.SetAction(pR =>
            {
                int id = pR.GetValue(idArgument);
                Relation relation = env.RelData.GetRelationById(id) ?? throw new ArgumentException($"{id} is not the id of a relation.");
                Console.WriteLine(relation.ToVerboseString(env.RelData));
            });

            Command listArts = new("articles", "List of articles in the database.");
            listCommand.Subcommands.Add(listArts);

            listArts.SetAction(pR =>
            {
                foreach (Article a in env.Articles.Values)
                {
                    if (a.Id < 0) { continue; }
                    Console.WriteLine(a);
                }
                Console.WriteLine("Other 'articles' may exist with ids less than 0. These are internal logical articles, such as ID-2 ('folklore') and ID-3 ('unknown').");
            });

            Command listThms = new("theorems", "List of theorems in the database.");
            listCommand.Subcommands.Add(listThms);

            listThms.SetAction(pR =>
            {
                foreach (Theorem t in env.Theorems.Values)
                {
                    if (t.Id < 0) { continue; }
                    Console.WriteLine(t);
                }
                Console.WriteLine("Other 'theorems' may exist with ids less than 0. These are internal logical theorems, such as ID-2 ('folklore') and ID-3 ('unknown').");
            });

            Command listModels = new("models", "List of models in the database.");
            listCommand.Subcommands.Add(listModels);

            listModels.SetAction(pR =>
            {
                foreach (Model m in env.Models.Values)
                {
                    Console.WriteLine(m);
                }
                return 0;
            });

            Command listBetween = new("between", "List all cardinals lying between two given ids.")
            {
                idArgument,
                idArgumentTwo
            };
            listCommand.Subcommands.Add(listBetween);
            listBetween.Aliases.Add("btwn");

            listBetween.SetAction(pR =>
            {
                env.ListBetween(pR.GetValue(idArgument), pR.GetValue(idArgumentTwo));
            });

            Command listTypes = new("types", "List all valid relation symbols.");
            listCommand.Subcommands.Add(listTypes);

            listTypes.SetAction(pR =>
            {
                List<char> printedTypes = [];

                string CtoCWriteString = "Cardinal-to-cardinal relations: ";
                foreach (char type in Sentence.CtoCTypes)
                {
                    CtoCWriteString += type + ", ";
                    printedTypes.Add(type);
                }
                Console.WriteLine(CtoCWriteString[..(Math.Min(0, CtoCWriteString.Length - 2))]);

                string MCNWriteString = "Model-cardinal-aleph relations: ";
                foreach (char type in Sentence.MCNTypes)
                {
                    MCNWriteString += type + ", ";
                    printedTypes.Add(type);
                }
                Console.WriteLine(MCNWriteString[..(Math.Min(0, MCNWriteString.Length - 2))]);

                if (Sentence.TypeIndices.Count == printedTypes.Count + 1) return;

                string UnexpectedTypes = "Unanticipated types (consider submitting a bug report): ";
                foreach (char type in Sentence.TypeIndices.Where(t => !t.Equals('X') && !printedTypes.Contains(t)))
                {
                    UnexpectedTypes += type + ", ";
                }
                Console.WriteLine(UnexpectedTypes[..(Math.Min(0, UnexpectedTypes.Length - 2))]);
            });

            // Search objects
            Command searchCommand = new("search", "Search data.");
            rootCommand.Subcommands.Add(searchCommand);

            // Search relations
            Command searchRelations = new("relations", "Search relations.")
            {
                containsCardinalsOption, // Only those pertaining to certain cardinals
                containsModelsOption, // Only those pertaining to certain models
                containsTypesOption, // Only those of certain prescribed types
                inArticlesOption, // etc.
                fromTheoremsOption,
                fromAgeSetOption,
                fromIdSetOption
            };
            searchCommand.Subcommands.Add(searchRelations);

            searchCommand.SetAction(pR =>
            {
                string[] cardinalSearch = pR.GetValue(containsCardinalsOption) ?? [];
                string[] modelSearch = pR.GetValue(containsModelsOption) ?? [];
                string[] typeSearch = pR.GetValue(containsTypesOption) ?? [];
                string[] articleSearch = pR.GetValue(inArticlesOption) ?? [];
                string[] theoremSearch = pR.GetValue(fromTheoremsOption) ?? [];
                string[] ageSearch = pR.GetValue(fromAgeSetOption) ?? [];
                string[] idSearch = pR.GetValue(fromIdSetOption) ?? [];
                List<Relation> relationsInSearch = env.SearchRelations(
                    cardinalSearch,
                    modelSearch,
                    typeSearch,
                    articleSearch,
                    theoremSearch,
                    ageSearch,
                    idSearch);
                Console.WriteLine($"{relationsInSearch.Count} relations found.");
                foreach (Relation relation in relationsInSearch)
                {
                    Console.WriteLine(relation);
                }
            });

            // Add objects
            Command createCommand = new("create", "Create a new specified object.");
            createCommand.Aliases.Add("add");
            rootCommand.Subcommands.Add(createCommand);

            // Add cardinal
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

            // Add relation
            // Needs some retouching
            Command createRelation = new("relation", "Add a relation by ids.")
            {
                witnessIdArgument,
                typeArgument,
                idsArgument
            };
            createCommand.Subcommands.Add(createRelation);

            createRelation.SetAction(pR =>
            {
                int[] ids = pR.GetValue(idsArgument) ?? throw new ArgumentException("You must include ids for the new relation");
                string typeString = pR.GetValue(typeArgument) ?? throw new ArgumentException("You must include a relation type");
                char type = typeString[0];
                int witnessId = pR.GetValue(witnessIdArgument);
                if (Sentence.CtoCTypes.Contains(type))
                {
                    if (ids.Length != 2) throw new ArgumentException($"Exactly two ids required for type {type} relations. You have provided {ids.Length}.");
                    env.RelateCtoC(ids[0], ids[1], type, witnessId);
                    if (Program._loadLog)
                    {
                        var c1 = env.RelData.GetCardinalById(ids[0]);
                        var c2 = env.RelData.GetCardinalById(ids[1]);
                        Console.WriteLine($"Cardinals {c1} and {c2} related with type '{type}' relation.");
                    }
                    return 0;
                }
                else if (Sentence.MCNTypes.Contains(type))
                {
                    if (ids.Length != 3) throw new ArgumentException($"Exactly three ids required for type {type} relations. You have provided {ids.Length}.");
                    env.RelateMCN(ids[0], ids[1], ids[2], type, witnessId);
                    if (Program._loadLog)
                    {
                        var m = env.RelData.GetModelById(ids[0]);
                        var c = env.RelData.GetCardinalById(ids[1]);
                        int n = ids[2];
                        Console.WriteLine($"New relation '{m} \\models {c} {type} \\aleph_{n}' added.");
                    }
                }
                else if (Sentence.TypeIndices.Contains(type))
                {
                    throw new NotImplementedException($"Type {type} not accounted for in programming (consider submitting a bug report).");
                }
                throw new ArgumentException($"{type} is not a valid relation type.");
            });

            // Add relation by ID *or* symbol string (best guess).
            Command relateIdOrSymbol = new("relateByDescription", "Creates a relation identifying cardinals by id, symbol or name (and models by id).")
            {
                witnessIdArgument,
                typeArgument,
                descriptionOfObjectsArgument
            };
            createCommand.Subcommands.Add(relateIdOrSymbol);

            relateIdOrSymbol.SetAction(pR =>
            {
                string typeString = pR.GetValue(typeArgument) ?? throw new ArgumentException("You must provide a relation type");
                char type = typeString[0];
                int witnessId = pR.GetValue(witnessIdArgument);
                string[] objectDescriptions = pR.GetValue(descriptionOfObjectsArgument) ?? throw new ArgumentException("You must provide the descriptions (ids, symbols or names) of objects being related.");
                Theorem theorem = env.RelData.GetTheoremById(witnessId) ?? throw new ArgumentException($"ID {witnessId} is not a valid theorem id");
                env.AddResultToTheoremByProperties(type, objectDescriptions, theorem);
            });

            // Add relations by an ideal
            Command createIdeal = new("ideal", "Creates four cardinal characteristics associated with an ideal and the automatic relations obtained from this.")
            {
                nameArgument,
                symbolArgument
            };
            createCommand.Subcommands.Add(createIdeal);

            createIdeal.SetAction(pR =>
            {
                string idealName = pR.GetValue(nameArgument) ?? throw new ArgumentException("You must include a name.");
                string idealSymbol = pR.GetValue(symbolArgument) ?? throw new ArgumentException("You must include a symbol.");
                (string name, string symbol)[] idealCardinals = [("Additivity", "add"), ("Covering", "cov"), ("Uniformity", "non"), ("Cofinality", "cof")];
                List<CC> newCardinals = [];
                foreach ((string name, string symbol) in idealCardinals)
                {
                    newCardinals.Add(env.AddCardinal($"{name} of {idealName}", $"{symbol}({idealSymbol})"));
                }
                (int, int)[] relationIndices = [(3, 2), (3, 1), (3, 0), (2, 0), (1, 0)];
                foreach ((int i, int j) in relationIndices)
                {
                    env.RelateCtoC(newCardinals[i].Id, newCardinals[j].Id, '>', -2);
                }
                Console.WriteLine($"Added four new cardinals associated with the {idealName} ideal.");
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

            // Add empty article
            Command createBlankArticle = new("barticle", "Add an empty article");
            createCommand.Subcommands.Add(createBlankArticle);

            createBlankArticle.SetAction(pR =>
            {
                env.AddArticle("Test", 99999999, "Test");
                Console.WriteLine("Added test article.");
            });

            // Add a result to a theorem
            Command createResultInTheorem = new("resultInTheorem", "Add a result to a theorem")
            {
                idArgument,
                typeArgument,
                idsArgument
            };
            createCommand.Subcommands.Add(createResultInTheorem);

            createResultInTheorem.SetAction(pR =>
            {
                int theoremId = pR.GetValue(idOption);
                string typeString = pR.GetValue(typeArgument) ?? "X";
                char type = typeString[0];
                int[] ids = pR.GetValue(idsArgument) ?? [];
                Theorem theorem = env.RelData.GetTheoremById(theoremId) ?? throw new ArgumentException($"ID {theoremId} is not a valid theorem id");
                env.AddResultToTheorem(theorem, type, ids);
            });

            // Add theorem
            Command createTheorem = new("theorem", "Create a new theorem in an article, with options to add related relatinons.")
            {
                articleIdArgument,
                descriptionArgument,
                idOption
            };
            createCommand.Subcommands.Add(createTheorem);
            createTheorem.Aliases.Add("thm");

            createTheorem.SetAction(async pR =>
            {
                int id = pR.GetValue(idOption);
                int articleId = pR.GetValue(articleIdArgument);
                string description = pR.GetValue(descriptionArgument) ?? "No description provided";
                Article article = env.RelData.GetArticleById(articleId) ?? throw new ArgumentException($"{articleId} is not a valid article id");
                Theorem newTheorem;
                if (id == -1)
                {
                    newTheorem = env.AddTheorem(article, description);
                }
                else
                {
                    newTheorem = env.AddTheorem(article, description, id);
                }
                // Maybe wrap all these queries into a static function in InteractiveShell?
                Console.Write("Would you like to add results to this theorem? [Y]es / [N]o (Default Yes): ");
                string response = Console.ReadLine() ?? "y";
                if (response.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || response.Equals("y", StringComparison.OrdinalIgnoreCase)
                || response.Equals(""))
                {
                    Console.WriteLine("Populating theorem. Type [type] [ids] and enter for each new theorem. Type 'exit' to stop or 'list' to list all cardinal characteristics.");
                    await InteractiveShell.RepeatCommand(relateIdOrSymbol, [newTheorem.Id.ToString()], "exit", [("list", listCC)]);
                    Console.Write("Would you like to instantiate these new results as AtomicRelations and Relations? [Y]es / [N]o (Default: Yes): ");
                    string atomResponse = Console.ReadLine() ?? "y";
                    // Flipping the order just for nesting readability
                    if (!atomResponse.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    && !atomResponse.Equals("y", StringComparison.OrdinalIgnoreCase)
                    && !atomResponse.Equals(""))
                    {
                        return;
                    }
                    env.RegenerateAtoms();
                }
            });

            // Add placeholder theorem
            Command createPlaceholderTheorem = new("ptheorem", "Add a placeholder theorem");
            createCommand.Subcommands.Add(createPlaceholderTheorem);

            createPlaceholderTheorem.SetAction(pR =>
            {
                int newId = RelationDatabase.NewDictId(env.Theorems);
                Theorem newTheorem = new(0, new(), [new('>', [1, 0]), new('>', [0, 9])], "Test");
                env.Theorems[newId] = newTheorem;
            });

            // Add model
            Command createModel = new("model", "Create a new model in an article, with further options to add related relations.")
            {
                articleIdArgument,
                descriptionArgument,
                idOption
            };
            createCommand.Subcommands.Add(createModel);
            createModel.SetAction(pR =>
            {
                int articleId = pR.GetValue(articleIdArgument);
                string description = pR.GetValue(descriptionArgument) ?? "No description provided";
                int id = pR.GetValue(idOption);
                Article article = env.RelData.GetArticleById(articleId) ?? throw new ArgumentException($"{articleId} is not a valid article id");
                if (id == -1)
                {
                    env.AddModel(article, description);
                }
                else
                {
                    env.AddModel(article, description, id);
                }
                return 0;
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

            // Load
            Command loadCommand = new("load", "(NOT IMPLEMENTED) Load the contents of a given file to the current relation environment.")
            {
                fileArgument,
                descriptionArgument
            };
            rootCommand.Subcommands.Add(loadCommand);

            loadCommand.SetAction(pR =>
            {
                string description = pR.GetValue(descriptionArgument) ?? "";
                string file = RelationEnvironment.AddExtension(
                    pR.GetValue(fileArgument) ?? throw new ArgumentException("You must include a file to load."),
                    ".json");
                string filePath = Path.Combine(env.LoadDirectory, file);
                throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code detected
                return 0; // This is to tell SetAction what kind of function we're feeding it.
#pragma warning restore CS0162 // Unreachable code detected
            });

            //Plot
            Command plotCommand = new("plot", "Draw the relations as a dot graph.") // IDs doesn't really work properly here
            {
                dotFiletypeArgument,
                fileOption,
                idsOption
            };
            rootCommand.Subcommands.Add(plotCommand);

            plotCommand.SetAction(pR =>
            {
                string dotArgument = pR.GetValue(dotFiletypeArgument) ?? "";
                string file = pR.GetValue(fileOption)!;
                int[] ids = pR.GetValue(idsOption)!;

                // Legacy functionality
                if (dotArgument == "--toPng")
                {
                    dotArgument = "-Tpng";
                }

                string extension = "png";
                if (dotArgument != "")
                {
                    extension = dotArgument[2..dotArgument.Length];
                }

                if (file != null)
                {
                    env.PlotGraphDot(ids, file);
                    if (dotArgument != "")
                    {
                        env.PlotGraphPng(RelationEnvironment.AddExtension(file, $".{extension}", "Graph save file"), file, dotArgument);
                    }
                }
                else
                {
                    env.PlotGraphDot(ids, env.DotFile);
                    if (dotArgument != "")
                    {
                        env.PlotGraphPng(env.DotFile, env.GraphFile, dotArgument);
                    }
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
                string zb = pR.GetValue(zbArgument) ?? throw new ArgumentException("You must include a zb identifier.");
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
                    InteractiveShell shell = new(env, rootCommand);
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
        public RelationDatabase RelData = new();
        public bool Unsaved { get; private set; } = false;
        public Dictionary<int, CC> Cardinals => RelData.Cardinals;
        public Dictionary<int, Article> Articles => RelData.Articles;
        public Dictionary<int, Theorem> Theorems => RelData.Theorems;
        public Dictionary<int, Model> Models => RelData.Models;
        public Dictionary<int, Relation> Relations => RelData.Relations;


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
            var LoadedTheorems = JsonFileHandler.LoadTheorems(ThmsPath, LoadedCardinals, LoadedArticles);
            Program.LoadLog("Loading models.");
            var LoadedModels = JsonFileHandler.LoadModels(ModsPath, LoadedCardinals, LoadedArticles, LoadedTheorems);
            Program.LoadLog("Loading relations.");
            var LoadedRelations = JsonFileHandler.LoadRelations(RelsPath, LoadedTheorems);
            Program.LoadLog("Creating Relation Database environment.");
            RelData = new RelationDatabase(LoadedCardinals, LoadedRelations, LoadedArticles, LoadedTheorems, LoadedModels);
            Program.LoadLog("Populating atomic relations.");
            int atomsCreated = RelData.GenerateAtoms();
            Program.LoadLog($"Created {atomsCreated} atoms.");
            Program.LoadLog("Generating trivial relations.");
            int trivialRelationsCreated = RelData.CreateTrivialRelations();
            Program.LoadLog($"{trivialRelationsCreated} new relations generated.");
            Program.LoadLog("Relation Environment complete.");
        }

        public RelationEnvironment() : this("cardinal_characteristics", "relations", "relations", "graph", "articles", "theorems", "models")
        {
        }
        public void RegenerateAtoms()
        {
            Program.LoadLog("Repopulating atomic relations.");
            int atomsCreated = RelData.GenerateAtoms();
            Program.LoadLog($"Created {atomsCreated} new atoms.");
            if (atomsCreated == 0) return;
            Program.LoadLog("Generating new trivial relations.");
            int trivialRelationsCreated = RelData.CreateTrivialRelations(true);
            Program.LoadLog($"{trivialRelationsCreated} new relations generated.\nConsider running 'trans'.");
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
                    Console.WriteLine($"Warning: {fileDescription} has extension other than {ext}. Programme will attempt to use {file}.{ext}");
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
            JsonFileHandler.Save(RelsPath, Relations);
            Program.LoadLog($"RelData saved to {RelsPath}.");
            JsonFileHandler.Save(ArtsPath, Articles);
            Program.LoadLog($"Articles saved to {ArtsPath}.");
            JsonFileHandler.Save(ThmsPath, Theorems);
            Program.LoadLog($"Theorems saved to {ThmsPath}.");
            JsonFileHandler.Save(ModsPath, Models);
            Program.LoadLog($"Models saved to {ModsPath}.");
        }

        public void AddCardinal(string? name, string? symbol, int id)
        {
            RelData.AddCardinal(name, symbol, id);
            Unsaved = true;
        }
        public CC AddCardinal(string? name, string? symbol)
        {
            CC newCardinal = RelData.AddCardinal(name, symbol);
            Unsaved = true;
            return newCardinal;
        }
        public void RelateCtoC(int idOne, int idTwo, char type, int witnessId)
        {
            RelData.AddCtoCRelationByIds(idOne, idTwo, type, witnessId);
            Unsaved = true;
        }
        public void RelateMCN(int modelId, int cardinalId, int aleph, char type, int witnessId)
        {
            RelData.AddMCNRelationByIds(modelId, cardinalId, aleph, type, witnessId);
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
        public Theorem AddTheorem(Article article, string description, int id)
        {
            Theorem newTheorem = RelData.AddTheorem(article, description, [], id);
            Unsaved = true;
            return newTheorem;
        }
        public Theorem AddTheorem(Article article, string description)
        {
            Theorem newTheorem = RelData.AddTheorem(article, description, []);
            Unsaved = true;
            return newTheorem;
        }
        public void AddResultToTheorem(Theorem theorem, char type, int[] ids)
        {
            if (RelationDatabase.AddResultToTheorem(theorem, type, ids))
            {
                Unsaved = true;
            }
        }

        public void AddResultToTheoremByProperties(char type, string[] objectDescriptions, Theorem theorem)
        {
            List<int> ids = RelData.sentenceIdsFromProperties(type, objectDescriptions);
            if (RelationDatabase.AddResultToTheorem(theorem, type, ids.ToArray()))
            {
                Unsaved = true;
            }
        }
        public void AddModel(Article article, string description, int id)
        {
            RelData.AddModel(article, description, id);
            Unsaved = true;
        }
        public void AddModel(Article article, string description)
        {
            RelData.AddModel(article, description);
            Unsaved = true;
        }
        public async Task ImportArticle(string zb)
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
            int numNewRels = RelData.LogicTransClose();
            if (numNewRels > 0) Unsaved = true;
        }

        public string PlotGraphDot(int[] ids, string fileName)
        {
            Dictionary<int, CC> cardinals = RelData.Cardinals
                .Where(kvp => ids.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            HashSet<HashSet<CC>> equivalenceClasses = GraphHandler.EquivalenceClasses(cardinals, RelData.Relations);
            IEnumerable<CC> transversal = equivalenceClasses.Select(equivalenceClass => equivalenceClass.First());
            cardinals = cardinals.Where(kvp => transversal.Contains(kvp.Value)).ToDictionary();
            var dot = GraphLogic.Vis.GraphDrawer.GenerateGraph(cardinals, RelData.GetMinimalRelations(cardinals), RelData);
            GraphLogic.Vis.GraphDrawer.WriteDotFile(dot, Path.Combine(OutDirectory, fileName));
            return dot;
        }

        public string PlotGraphDot(int[] ids)
        {
            return PlotGraphDot(ids, DotFile);
        }
        public void PlotGraphPng(string dotFileName, string fileName, string dotArgument)
        {
            string extension = dotArgument[2..dotArgument.Length];
            int lastFullStop = fileName.LastIndexOf('.');
            string filePrefix = fileName[..lastFullStop];
            string fileSuffix = fileName[(lastFullStop + 1)..fileName.Length];
            if (fileSuffix != extension)
            {
                Console.WriteLine($"Warning: The extension given ({fileSuffix}) does not match the given suffix argument ({extension}).");
                Console.Write($"Save to {filePrefix}.{extension} instead? [Y]es/[N]o (Default: Yes):");
                string response = Console.ReadLine() ?? "y";
                string[] validResponses = ["y", "n", "yes", "no"];
                do
                {
                    if (response.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = $"{filePrefix}.{extension}";
                    }
                    else if (response.Equals("n", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    else
                    {
                        Console.Write("Response not recognised, try again: ");
                        response = Console.ReadLine() ?? "y";
                    }
                }
                while (!validResponses.Contains(response.ToLower()));
            }
            GraphLogic.Vis.GraphDrawer.WritePngFile(OutDirectory, dotFileName, OutDirectory, fileName, dotArgument);
        }
        public void PopulateDensity()
        {
            if (RelData.PopulateDensity()) Unsaved = true;
        }
        public void ListBetween(int id1, int id2)
        {
            if (!RelData.DynamicDensity)
            {
                Console.WriteLine("In betweenness relation not yet calculated. Try running --btwn first.");
                return;
            }
            CC cardinal1 = RelData.GetCardinalById(id1)!;
            CC cardinal2 = RelData.GetCardinalById(id2)!;
            if (cardinal1 == null || cardinal2 == null)
            {
                throw new ArgumentException("One of the input ids returns a null cardinal");
            }
            if (!RelData.Density.TryGetValue((id1, id2), out HashSet<int>? density))
            {
                Console.WriteLine($"There are no cardinals between {cardinal1} and {cardinal2}");
                return;
            }
            if (density.Count == 0)
            {
                Console.WriteLine($"There are no cardinals between {cardinal1} and {cardinal2}");
                return;
            }
            if (density.Count == 1)
            {
                Console.WriteLine($"There is 1 cardinal between {cardinal1} and {cardinal2}");
            }
            else
            {
                Console.WriteLine($"There are {density.Count} cardinals between {cardinal1} and {cardinal2}");
            }
            foreach (int cardinalId in density)
            {
                Console.WriteLine(RelData.GetCardinalById(cardinalId));
            }
        }
        public List<Relation> SearchRelations(string[] cardinalSearch, string[] modelSearch,
            string[] typeSearch, string[] articleSearch, string[] theoremSearch,
            string[] ageSearch, string[] idSearch)
        {
            (HashSet<int> CtoCItem1Ids, HashSet<int> CtoCItem2Ids, HashSet<int> MCNCardinalIds, HashSet<int> OtherCardinalIds) validCardinalIds = RelData.CardinalIdsSearch(cardinalSearch);
            HashSet<int> modelIds = RelData.ModelIdsSearch(modelSearch);
            HashSet<char> validTypes = RelData.TypesSearch(typeSearch);
            HashSet<int> articleIds = RelData.ArticleIdsSearch(articleSearch);
            HashSet<int> theoremIds = RelData.TheoremIdsSearch(theoremSearch);
            HashSet<int> validAges = RelData.AgeSetSearch(ageSearch);
            HashSet<int> validIds = RelData.RelationIdsSearch(idSearch);
            List<Relation> outRelations = [];
            foreach (Relation relation in Relations.Values)
            {
                if (!validTypes.Contains(relation.Type)) { continue; }
                if (Sentence.CtoCTypes.Contains(relation.Type))
                {
                    if (!validCardinalIds.CtoCItem1Ids.Contains(relation.Item1Id)
                        || !validCardinalIds.CtoCItem2Ids.Contains(relation.Item2Id)) { continue; }
                }
                if (Sentence.MCNTypes.Contains(relation.Type))
                {
                    if (!validCardinalIds.MCNCardinalIds.Contains(relation.CardinalId)) { continue; }
                    if (!modelIds.Contains(relation.ModelId)) { continue; }
                }
                else
                {
                    if (!validCardinalIds.OtherCardinalIds.Contains(relation.CardinalId)) { continue; }
                }
                // This is union, so a relation touching any of the described theorems will be valid
                if (relation.Derivation.All(atom => !theoremIds.Contains(atom.WitnessId))) { continue; }
                if (!validAges.Contains(relation.Birthday)) { continue; }
                if (!validIds.Contains(relation.Id)) { continue; }
                outRelations.Add(relation);
            }
            return outRelations;
            throw new NotImplementedException();
        }
    }
    public class InteractiveShell
    {
        private bool ShouldExit = false;
        private readonly RelationEnvironment env;
        private readonly RootCommand rootCommand;
        private static readonly HttpClient Client = new();
        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true }; 
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
                string[] args = [.. CommandLineParser.SplitCommandLine(input).Prepend("--fromShell")];
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
        public static async Task RepeatCommand(Command command, string[] prependArgs, string exitCode, (string commandPhrase, Command command)[] otherCommands)
        {
            bool shouldLeaveRepeatCommand = false;
            while (!shouldLeaveRepeatCommand)
            {
                Console.Write(">> ");
                string input = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input == exitCode)
                {
                    break;
                }
                else
                {
                    string[] inputArgs = CommandLineParser.SplitCommandLine(input).ToArray();
                    if (inputArgs.Length == 0)
                    {
                        continue;
                    }
                    bool isOtherCommand = false;
                    foreach (var (commandPhrase, commandExecution) in otherCommands)
                    {
                        if (commandPhrase.Equals(inputArgs[0], StringComparison.OrdinalIgnoreCase))
                        {
                            await commandExecution.Parse(inputArgs.Skip(1).ToArray()).InvokeAsync();
                            isOtherCommand = true;
                            break;
                        }
                    }
                    if (isOtherCommand) continue;
                    string[] args = [.. prependArgs, .. inputArgs];
                    await command.Parse(args).InvokeAsync();
                }
            }
        }
    }
}