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

namespace CCview.CLI
{
    public class Program
    {
        public static void Log(string message)
        {
            Console.WriteLine($"Log: {message}.");
        }
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
            RootCommand rootCommand = CLI_old_commands.CreateRootCommand(env);
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
        public string RelationsPath { get; set; }
        public string ArticlesPath { get; set; }
        public string ThmsPath { get; set; }
        public string ModelsPath { get; set; }
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
            LoadDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../../CCview.Assets/"));
            OutDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, @"../../../output/"));

            CCFile = AddExtension(ccFile, ".json", "Cardinal characteristics file");
            RelsFile = AddExtension(relsFile, ".json", "RelData file");
            DotFile = AddExtension(dotFile, ".dot", "Dot file");
            GraphFile = AddExtension(graphFile, ".png", "Graph file");
            ArtsFile = AddExtension(artsFile, ".json", "Articles file");
            ThmsFile = AddExtension(thmsFile, ".json", "Theorems file");
            ModsFile = AddExtension(modsFile, ".json", "Models file");

            CCPath = Path.Combine(LoadDirectory, CCFile);
            RelationsPath = Path.Combine(LoadDirectory, RelsFile);
            DotPath = Path.Combine(OutDirectory, DotFile);
            GraphPath = Path.Combine(OutDirectory, GraphFile);
            ArticlesPath = Path.Combine(LoadDirectory, ArtsFile);
            ThmsPath = Path.Combine(LoadDirectory, ThmsFile);
            ModelsPath = Path.Combine(LoadDirectory, ModsFile);

            //LoadedCardinals = JsonInterface.LoadCardinals(CCPath);
            Program.LoadLog("Loading cardinals.");
            var LoadedCardinals = CardinalParser.LoadCardinals(CCPath);
            Program.LoadLog("Loading articles.");
            var LoadedArticles = ArticleParser.LoadArticles(ArticlesPath);
            Program.LoadLog("Loading theorems.");
            var LoadedTheorems = TheoremParser.LoadTheorems(ThmsPath, LoadedArticles);
            Program.LoadLog("Loading models.");
            var LoadedModels = ModelParser.LoadModels(ModelsPath);
            Program.LoadLog("Loading relations.");
            var LoadedRelations = RelationParser.LoadRelations(RelationsPath, LoadedTheorems);
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
            JArray cardinalsArray = CardinalParser.CardinalsToJArray(Cardinals);
            JsonUtils.Save(cardinalsArray, CCPath);
            Program.LoadLog($"Cardinals saved to {CCPath}.");

            JArray relationsArray = RelationParser.RelationsToJArray(Relations);
            JsonUtils.Save(relationsArray, RelationsPath);
            Program.LoadLog($"RelData saved to {RelationsPath}.");

            JArray articlesArray = ArticleParser.ArticlesToJArray(Articles);
            JsonUtils.Save(articlesArray, ArticlesPath);
            Program.LoadLog($"Articles saved to {ArticlesPath}.");

            JArray theoremsArray = TheoremParser.TheoremsToJArray(Theorems);
            JsonUtils.Save(theoremsArray, ThmsPath);
            Program.LoadLog($"Theorems saved to {ThmsPath}.");

            JArray modelsArray = ModelParser.ModelsToJArray(Models);
            JsonUtils.Save(modelsArray, ModelsPath);
            Program.LoadLog($"Models saved to {ModelsPath}.");

            Unsaved = false;
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
            if (RelData.AddResultToTheoremByProperties(theorem, type, objectDescriptions))
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
            Article article = ArticleParser.Deserialise(json);
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
            var dot = GraphDrawer.GenerateGraph(cardinals, RelData.GetMinimalRelations(cardinals), RelData);
            GraphDrawer.WriteDotFile(dot, Path.Combine(OutDirectory, fileName));
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
            GraphDrawer.WritePngFile(OutDirectory, dotFileName, OutDirectory, fileName, dotArgument);
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
                switch (relation.Relationship.GetFamily())
                {
                    case "CtoC":
                        if (!validCardinalIds.CtoCItem1Ids.Contains(relation.Item1Id)
                        || !validCardinalIds.CtoCItem2Ids.Contains(relation.Item2Id)) { continue; }
                        break;
                    case "MCN":
                        if (!validCardinalIds.MCNCardinalIds.Contains(relation.CardinalId)) { continue; }
                        if (!modelIds.Contains(relation.ModelId)) { continue; }
                        break;
                    default:
                        if (!validCardinalIds.OtherCardinalIds.Contains(relation.CardinalId)) { continue; }
                        break;
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