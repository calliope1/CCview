// This gets the commands out of Program.cs while we work on the new CLI.

using CCview.Core.DataClasses;
using CCview.Core.GraphLogic;
using CCview.Core.JsonHandler;
using CCview.Core.Services;
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
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using ICC = CCview.Core.Interfaces.ICardinalCharacteristic;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler.DataParsers;
using CCview.Core.Interfaces;
using CCview.CLI.Commands;

namespace CCview.CLI.Commands
{
    internal class CLI_old_commands
    {
        public static RootCommand CreateRootCommand(RelationEnvironment env)
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

            listRels.SetAction((Func<ParseResult, int>)(pR =>
            {
                if (!pR.GetValue(noSymbolsOption))
                {
                    foreach (Relation r in env.RelData.Relations.Values)
                    {
                        Console.WriteLine(r.ToStringWithSymbols<CC>((IReadOnlyDictionary<int, CC>)env.RelData.Cardinals));
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
            }));

            Command listRelation = new("relation", "Get more information about a given relation.")
            {
                idArgument
            };
            listCommand.Subcommands.Add(listRelation);

            listRelation.SetAction((Action<ParseResult>)(pR =>
            {
                int id = pR.GetValue(idArgument);
                Relation relation = env.RelData.GetRelationById(id) ?? throw new ArgumentException($"{id} is not the id of a relation.");
                Console.WriteLine(relation.ToVerboseString(env.RelData));
            }));

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
                foreach (char type in RelationType.CtoCTypes)
                {
                    CtoCWriteString += type + ", ";
                    printedTypes.Add(type);
                }
                Console.WriteLine(CtoCWriteString[..(Math.Min(0, CtoCWriteString.Length - 2))]);

                string MCNWriteString = "Model-cardinal-aleph relations: ";
                foreach (char type in RelationType.MCNTypes)
                {
                    MCNWriteString += type + ", ";
                    printedTypes.Add(type);
                }
                Console.WriteLine(MCNWriteString[..(Math.Min(0, MCNWriteString.Length - 2))]);

                if (RelationType.AnticipatedTypes.Count == printedTypes.Count + 1) return;

                string UnexpectedTypes = "Unanticipated types (consider submitting a bug report): ";
                foreach (char type in RelationType.AnticipatedTypes.Where(t => !t.Equals('X') && !printedTypes.Contains(t)))
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
                if (RelationType.CtoCTypes.Contains(type))
                {
                    if (ids.Length != 2) throw new ArgumentException($"Exactly two ids required for type {type} relations. You have provided {ids.Length}.");
                    env.RelateCtoC(ids[0], ids[1], type, witnessId);
                    Logging.LogDebug($"Cardinals {env.RelData.GetCardinalById(ids[0])} and {env.RelData.GetCardinalById(ids[1])} related with type '{type}' relation.");
                    return 0;
                }
                else if (RelationType.MCNTypes.Contains(type))
                {
                    if (ids.Length != 3) throw new ArgumentException($"Exactly three ids required for type {type} relations. You have provided {ids.Length}.");
                    env.RelateMCN(ids[0], ids[1], ids[2], type, witnessId);
                    Logging.LogDebug($"New relation '{env.RelData.GetModelById(ids[0])} \\models {env.RelData.GetCardinalById(ids[1])} {type} \\aleph_{ids[2]}' added.");
                    return 0;
                }
                else if (RelationType.AnticipatedTypes.Contains(type))
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
                    env.RelateCtoC(newCardinals[i].GetId(), newCardinals[j].GetId(), '>', -2);
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
            return rootCommand;
        }
    }
}
