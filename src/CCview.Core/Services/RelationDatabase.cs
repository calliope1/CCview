using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using ICC = CCview.Core.Interfaces.ICardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Services;
using CCview.Core.Interfaces;

namespace CCview.Core.Services
{
    public class RelationDatabase : IRelationDatabase
    {
        public Dictionary<int, CC> Cardinals { get; private set; } = [];
        public IReadOnlyDictionary<int, CC> GetCardinals()
        {
            return Cardinals;
        }
        public HashSet<AtomicRelation> AtomicRelations { get; private set; } = [];
        public HashSet<AtomicRelation> GetAtomicRelations()
        {
            return AtomicRelations;
        }
        public Dictionary<int, Relation> Relations { get; private set; } = [];
        public IReadOnlyDictionary<int, Relation> GetRelations()
        {
            return Relations;
        }
        private Dictionary<Sentence, (Relation Oldest, Relation Shortest)> OldestAndShortestRelations { get; set; } = [];
        public Dictionary<int, Article> Articles { get; private set; } = [];
        public IReadOnlyDictionary<int, Article> GetArticles()
        {
            return Articles;
        }
        public Dictionary<int, Theorem> Theorems { get; private set; } = [];
        public IReadOnlyDictionary<int, Theorem> GetTheorems()
        {
            return Theorems;
        }
        public Dictionary<int, Model> Models { get; private set; } = [];
        public IReadOnlyDictionary<int, Model> GetModels()
        {
            return Models;
        }
        public Dictionary<(int LargerId, int SmallerId), HashSet<int>> Density { get; private set; } = [];
        public IReadOnlyDictionary<(int LargerId, int SmallerId), HashSet<int>> GetDensity()
        {
            return Density;
        }
        public bool DynamicDensity { get; private set; } = false;
        private bool TrivialRelationsCreated { get; set; } = false;

        public RelationDatabase(
            IReadOnlyDictionary<int, CC> cardinals,
            IReadOnlyDictionary<int, Relation> relations,
            IReadOnlyDictionary<int, Article> articles,
            IReadOnlyDictionary<int, Theorem> theorems,
            IReadOnlyDictionary<int, Model> models
            )
        {
            Cardinals = cardinals.ToDictionary();
            Relations = relations.ToDictionary();
            Articles = articles.ToDictionary();
            Theorems = theorems.ToDictionary();
            Models = models.ToDictionary();
            foreach (var relation in Relations.Values)
            {
                UpdateOldestAndShortest(relation);
            }
        }
        public RelationDatabase()
        {
        }
        private (bool, bool) CheckOldestAndShortest(Relation relation)
        {
            if (OldestAndShortestRelations.TryGetValue(relation.Statement, out var oldestAndShortestRelations))
            {
                bool inputIsOlder = false;
                bool inputIsShorter = false;
                if (relation.Birthday < oldestAndShortestRelations.Oldest.Birthday)
                {
                    inputIsOlder = true;
                }
                if (relation.Derivation.Count < oldestAndShortestRelations.Shortest.Derivation.Count)
                {
                    inputIsShorter = true;
                }
                return (inputIsOlder, inputIsShorter);
            }
            else
            {
                OldestAndShortestRelations[relation.Statement] = (relation, relation);
                return (true, true);
            }
        }
        private (bool IsOlder, bool IsShorter) UpdateOldestAndShortest(Relation relation)
        {
            if (OldestAndShortestRelations.TryGetValue(relation.Statement, out var oldestAndShortestRelations))
            {
                bool inputIsOlder = false;
                bool inputIsShorter = false;
                if (relation.Birthday < oldestAndShortestRelations.Oldest.Birthday)
                {
                    OldestAndShortestRelations[relation.Statement] = (relation, oldestAndShortestRelations.Shortest);
                    inputIsOlder = true;
                }
                if (relation.Derivation.Count < oldestAndShortestRelations.Shortest.Derivation.Count)
                {
                    OldestAndShortestRelations[relation.Statement] = (oldestAndShortestRelations.Oldest, relation);
                    inputIsShorter = true;
                }
                return (inputIsOlder, inputIsShorter);
            }
            else
            {
                OldestAndShortestRelations[relation.Statement] = (relation, relation);
                return (true, true);
            }
        }
        public bool PopulateDensity(bool overrideChecks = false, bool fromDeductiveClosure = false)
        {
            if (DynamicDensity && !overrideChecks) Logging.LogDebug("In-betweenness relation already instantiated.");
            else
            {
                if (!overrideChecks)
                {
                    Logging.LogDebug("Computing in-betweenness relation for cardinal characteristics.");
                    if (!fromDeductiveClosure)
                    {
                        Logging.LogDebug("First computing transitive closure.");
                    }
                }
                int n = 0;
                if (!fromDeductiveClosure)
                {
                    n = LogicTransClose();
                }
                foreach (Relation r1 in Relations.Values)
                {
                    if (r1.Relationship.Symbol.Equals('>'))
                    {
                        foreach (Relation r2 in Relations.Values)
                        {
                            if (r2.Relationship.Symbol.Equals('>'))
                            {
                                if (r1.Item2Id.Equals(r2.Item1Id))
                                {
                                    if (Density.TryGetValue((r1.Item1Id, r2.Item2Id), out HashSet<int>? between))
                                    {
                                        between.Add(r1.Item2Id);
                                    }
                                    else
                                    {
                                        Density[(r1.Item1Id, r2.Item2Id)] = [r1.Item2Id];
                                    }
                                }
                            }
                        }
                    }
                }
                if (!overrideChecks)
                {
                    Logging.LogDebug("In-betweenness relation computed.");
                    Logging.LogDebug("The in-betweenness relation will continue to compute as the database is modified. This will also automatically maintain transitive closure.");
                }
                DynamicDensity = true;
                return n > 0;
            }
            return false;
        }
        public int CreateTrivialRelations(bool overrideCheck = false)
        {
            if (TrivialRelationsCreated && !overrideCheck) return 0;
            int numberOfRelations = Relations.Count;
            foreach (AtomicRelation a in AtomicRelations)
            {
                Relation newRel = new(a, NewDictId(Relations));
                if (!Relations.Any(r => r.Value.Equals(newRel)))
                {
                    Relations[newRel.Id] = newRel;
                    UpdateOldestAndShortest(newRel);
                }
            }
            TrivialRelationsCreated = true;
            return Relations.Count - numberOfRelations;
        }
        /// <summary>
        /// Creates a copy of the given relation in the Relations dictionary, changing
        /// the id if necessary to avoid conflicts.
        /// </summary>
        /// <param name="relation"></param>
        private void CopyRelationIntoDictionary(Relation relation)
        {
            int newId = relation.Id;
            if (newId == -1 || Relations.ContainsKey(newId))
            {
                newId = NewDictId(Relations);
            }
            Relation newRelation = new(newId, relation.Statement, relation.Derivation);
            Relations[newId] = newRelation;
        }
        private HashSet<Relation> InternalComputeDeductiveClosure()
        {
            HashSet<Relation> newRelations = [];
            HashSet<Relation> iterationRelations = Relations.Values.ToHashSet();
            bool changed;
            do
            {
                changed = false;
                HashSet<Relation> newRelationsToAdd = [];
                foreach (var relationOne in iterationRelations)
                {
                    foreach (var relationTwo in iterationRelations)
                    {
                        Relation? deducedRelation = relationOne.Deduce(relationTwo);
                        if (deducedRelation is Relation newRelation)
                        {
                            var (isOlder, isShorter) = UpdateOldestAndShortest(newRelation);
                            if (isOlder || isShorter)
                            {
                                newRelationsToAdd.Add(newRelation);
                                changed = true;
                            }
                        }
                    }
                }
                foreach (Relation newRelation in newRelationsToAdd)
                {
                    iterationRelations.Add(newRelation);
                    newRelations.Add(newRelation);
                }
            } while (changed);
            // This may include some non-oldest/non-shortest relations, but they are possibly necessary for the deduction of the oldest/shortest relations elsewhere.
            foreach (Relation newRelation in newRelations)
            {
                CopyRelationIntoDictionary(newRelation);
            }
            if (DynamicDensity)
            {
                PopulateDensity(true, true);
            }
            return newRelations;
        }
        //public static HashSet<Relation> ComputeDeductiveClosure(Dictionary<int, Relation> relations)
        //{
        //    HashSet<Relation> newRelations = [];
        //    HashSet<Relation> iterRelations = relations.Values.ToHashSet();
        //    Dictionary<Sentence, int> ageDict = [];
        //    foreach (Relation r in relations.Values)
        //    {
        //        if (ageDict.TryGetValue(r.Statement, out int age))
        //        {
        //            ageDict[r.Statement] = Math.Min(age, r.Birthday);
        //        }
        //        else
        //        {
        //            ageDict[r.Statement] = r.Birthday;
        //        }
        //    }
        //    bool changed;
        //    do
        //    {
        //        changed = false;
        //        HashSet<Relation> newRelationsToAdd = [];
        //        foreach (var r1 in iterRelations)
        //        {
        //            foreach (var r2 in iterRelations)
        //            {
        //                Relation? dedRel = r1.Deduce(r2);
        //                if (dedRel is Relation newRel)
        //                {
        //                    if (ageDict.TryGetValue(newRel.Statement, out int age))
        //                    {
        //                        if (newRel.Birthday < age)
        //                        {
        //                            newRelationsToAdd.Add(newRel);
        //                            ageDict[newRel.Statement] = newRel.Birthday;
        //                            changed = true;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        newRelationsToAdd.Add(newRel);
        //                        ageDict[newRel.Statement] = newRel.Birthday;
        //                        changed = true;
        //                    }
        //                }
        //            }
        //        }
        //        foreach (var r in newRelationsToAdd)
        //        {
        //            iterRelations.Add(r);
        //            newRelations.Add(r);
        //        }
        //    } while (changed);
        //    return newRelations;
        //}
        public int LogicTransClose()
        {
            int numberOfNewRelations = InternalComputeDeductiveClosure().Count;
            Logging.LogDebug($"Constructed {numberOfNewRelations} new relations in deductive closure.");
            return numberOfNewRelations;
        }
        public Relation AddCtoCRelation(CC? a, CC? b, char type, Theorem? witness)
        {
            if (!RelationType.IsCtoC(type)) throw new ArgumentException($"AddCtoCRelation cannot be called with type {type}.");
            else if (a == null || b == null) throw new ArgumentException("Neither cardinal may be null.");
            else if (witness == null) throw new ArgumentException("The theorem may not be null.");
            else
            {
                Sentence statement = new(type, [a.Id, b.Id]);
                AtomicRelation atom = new(statement, witness);
                Relation newRelation = new(atom, NewDictId(Relations));
                Relations[newRelation.Id] = newRelation;
                UpdateOldestAndShortest(newRelation);
                return newRelation;
            }
        }
        public Relation AddMCNRelation(Model? m, CC? c, int n, char type, Theorem? witness)
        {
            if (!RelationType.IsMCN(type)) throw new ArgumentException($"AddMCNRelation cannot be called with type {type}.");
            if (m == null) throw new ArgumentException("The model cannot be null.");
            if (c == null) throw new ArgumentException("The cardinal cannot be null.");
            if (witness == null) throw new ArgumentException("The theorem cannot be null.");
            Sentence statement = new(type, [m.Id, c.Id, n]);
            AtomicRelation atom = new(statement, witness);
            Relation newRelation = new(atom, NewDictId(Relations));
            Relations[newRelation.Id] = newRelation;
            UpdateOldestAndShortest(newRelation);
            return newRelation;
        }
        public static int NewDictId<T>(Dictionary<int, T> dict, bool fast = false)
        {
            if (fast)
            {
                return dict.Keys.Max() + 1;
            }
            var newId = 0;
            while (dict.ContainsKey(newId))
            {
                newId++;
            }
            return newId;
        }
        public Article AddArticle(Article article)
        {
            if (article.Id == -1)
            {
                int id = NewDictId(Articles);
                Article newArticle = new(id, article.Date, article.Name, article.Citation);
                Articles[id] = newArticle;
                return newArticle;
            }
            Articles[article.Id] = article;
            return article;
        }
        public Article AddArticle(string? name, int date, string? citation, int id)
        {
            if (Articles.ContainsKey(id)) // Order of operations is important here or you'll get errors for id >= CCI.Count
            {
                throw new ArgumentException($"ID {id} is in use by {GetArticleById(id)}.");
            }
            Article art = new(id, date, name ?? "No title provided", citation ?? "No citation provided");
            Articles[id] = art;
            return art;
        }
        public Article AddArticle(string? name, int date, string? citation)
        {
            int newId = NewDictId(Articles);
            return AddArticle(name, date, citation, newId);
        }
        public CC AddCardinal(string? name, string? symbol, int id)
        {
            if (Cardinals.ContainsKey(id))
            {
                throw new ArgumentException($"ID {id} is in use by {GetCardinalById(id)}.");
            }
            if (Cardinals.Values.Any(cardinal => cardinal.EquationSymbol.Equals(symbol)))
            {
                CC otherCardinal = GetCardinalBySymbol(symbol!);
                Console.WriteLine($"Warning: Another cardinal {otherCardinal} with the symbol {symbol} already exists.");
            }
            if (Cardinals.Values.Any(cardinal => cardinal.Name.Equals(name)))
            {
                CC otherCardinal = GetCardinalByName(name!);
                Console.WriteLine($"Warning: Another cardinal {otherCardinal} with the name {name} already exists.");
            }
            var newCardinal = new CC(id, name!, symbol!);
            Cardinals[id] = newCardinal;
            Console.WriteLine($"Added new cardinal: {newCardinal}");
            return newCardinal;
        }
        public CC AddCardinal(string? name, string? symbol, bool fast = false)
        {
            return AddCardinal(name, symbol, NewDictId(Cardinals, fast));
        }
        public Theorem AddTheorem(Article article, string description, HashSet<Sentence> results, int id)
        {
            if (Theorems.ContainsKey(id))
            {
                throw new ArgumentException($"ID {id} is in use by {GetTheoremById(id)}.");
            }
            Theorem newTheorem = new(id, article, results, description);
            Theorems[id] = newTheorem;
            Console.WriteLine($"Added new theorem: {newTheorem}");
            return newTheorem;
        }
        public Theorem AddTheorem(Article article, string description, HashSet<Sentence> results)
        {
            int newId = NewDictId(Theorems);
            return AddTheorem(article, description, results, newId);
        }
        public static bool AddResultToTheorem(Theorem theorem, char type, int[] ids)
        {
            Sentence newResult = new(type, ids.ToList());
            if (theorem.Results.Contains(newResult))
            {
                Console.WriteLine($"Result {newResult} already exists in {theorem}");
                return false;
            }
            theorem.Results.Add(newResult);
            Console.WriteLine($"Added new result {newResult} to {theorem}");
            return true;
        }
        public bool AddResultToTheoremByProperties(Theorem theorem, char type, string[] objectDescriptions)
        {
            int[] ids = SentenceIdsFromProperties(type, objectDescriptions);
            return AddResultToTheorem(theorem, type, ids);
        }
        public Model AddModel(Article article, string? description, int id)
        {
            if (Models.ContainsKey(id))
            {
                throw new ArgumentException($"ID {id} is in use by {GetModelById(id)}.");
            }
            Model newModel = new(id, article, description ?? "No description provided!", []);
            Models[id] = newModel;
            Console.WriteLine($"Added new model: {newModel}");
            return newModel;
        }
        public Model AddModel(Article article, string description)
        {
            int newId = NewDictId(Models);
            return AddModel(article, description, newId);
        }
        public HashSet<Relation> GetMinimalRelations(IReadOnlyDictionary<int, CC> desiredCardinals)
        {
            return DynamicDensity
                ? GraphAlgorithm.DensityTransitiveReduction(desiredCardinals, Relations, Density)
                : GraphAlgorithm.TransitiveReduction(desiredCardinals, Relations);
        }
        public static T? GetTByIdOrDefault<T>(Dictionary<int, T> dict, int id, T? defaultValue)
        {
            if (dict.TryGetValue(id, out T? match))
            {
                return match;
            }
            else
            {
                Console.WriteLine($"WARNING: No object type {typeof(T)} with id {id} found. Returning null.");
                return defaultValue;
            }
        }
        public CC? GetCardinalById(int id)
        {
            return GetTByIdOrDefault(Cardinals, id, null);
        }

        public Relation? GetRelationById(int id)
        {
            return GetTByIdOrDefault(Relations, id, null);
        }
        public Article? GetArticleById(int id)
        {
            return GetTByIdOrDefault(Articles, id, null);
        }
        public Theorem? GetTheoremById(int id)
        {
            return GetTByIdOrDefault(Theorems, id, null);
        }
        public Model? GetModelById(int id)
        {
            return GetTByIdOrDefault(Models, id, null);
        }
        public bool AddCtoCRelationByIds(int id1, int id2, char type, int witnessId)
        {
            Relation newRelation = AddCtoCRelation(GetCardinalById(id1), GetCardinalById(id2), type, GetTheoremById(witnessId));
            return Relations.TryGetValue(newRelation.Id, out Relation? relation) && newRelation.Equals(relation);
        }
        public bool AddMCNRelationByIds(int modId, int ccId, int n, char type, int witnessId)
        {
            Relation newRelation = AddMCNRelation(GetModelById(modId), GetCardinalById(ccId), n, type, GetTheoremById(witnessId));
            return Relations.TryGetValue(newRelation.Id, out Relation? relation) && newRelation.Equals(relation);
        }
        private int[] SentenceIdsFromProperties(char type, string[] objectDescriptions)
        {
            List<int> ids = [];
            if (RelationType.IsCtoC(type))
            {
                if (objectDescriptions.Length != 2)
                {
                    throw new ArgumentException($"Must include exactly two cardinal descriptions for cardinal-to-cardinal relation type {type}.");
                }
                ids = objectDescriptions.Select(description =>
                {
                    if (int.TryParse(description, out int result)) { return result; }
                    CC? match = CardinalFromProperties(description);
                    return match == null ? throw new ArgumentException($"No cardinal with description {description} exists.") : match.Id;
                }).ToList();
                return ids.ToArray();
            }
            if (RelationType.IsMCN(type))
            {
                if (objectDescriptions.Length != 3)
                {
                    throw new ArgumentException($"Must include a model id, a cardinal description and an integer for model-cardinal-number relation type {type}.");
                }
                ids.Add(int.Parse(objectDescriptions[0]));
                int cardinalId;
                if (int.TryParse(objectDescriptions[1], out int result)) { cardinalId = result; }
                else
                {
                    CC? match = CardinalFromProperties(objectDescriptions[1]);
                    cardinalId = match == null ? throw new ArgumentException($"No cardinal with description {objectDescriptions[1]} exists.") : match.Id;
                }
                ids.Add(cardinalId);
                int aleph = int.Parse(objectDescriptions[2]);
                ids.Add(aleph);
                return ids.ToArray();
            }
            if (type == 'X')
            {
                return [];
            }
            throw new ArgumentException($"Unexpected type {type} used in argument.");
        }
        public CC? CardinalBySymbolOrNull(string symbol)
        {
            return Cardinals.Values.FirstOrDefault(cardinal => cardinal!.EquationSymbol.Equals(symbol), null);
        }
        public CC GetCardinalBySymbol(string symbol)
        {
            CC? match = Cardinals.Values.FirstOrDefault(c => c.EquationSymbol == symbol);
            if (match != null)
            {
                return match;
            }
            else
            {
                throw new ArgumentException($"No cardinal with symbol {symbol} found.");
            }
        }
        public CC? CardinalByNameOrNull(string name)
        {
            return Cardinals.Values.FirstOrDefault(cardinal => cardinal!.Name.Equals(name), null);
        }
        public CC GetCardinalByName(string name)
        {
            CC? match = Cardinals.Values.FirstOrDefault(cardinal => cardinal.Name.Equals(name));
            if (match != null)
            {
                return match;
            }
            else
            {
                throw new ArgumentException($"No cardinal with name {name} found.");
            }
        }
        public int GenerateAtoms()
        {
            int numberOfAtoms = AtomicRelations.Count;
            foreach (Theorem thm in Theorems.Values)
            {
                AtomicRelations.UnionWith(GenerateAtoms(thm));
            }
            return AtomicRelations.Count - numberOfAtoms;
        }
        private static HashSet<AtomicRelation> GenerateAtoms(Theorem theorem)
        {
            HashSet<AtomicRelation> newAtoms = [];
            foreach (var sentence in theorem.Results)
            {
                newAtoms.Add(new(sentence, theorem));
            }
            return newAtoms;
        }
        /// <summary>
        /// Get a cardinal from one of its properties, checking (in order): ID number, symbol, and name.
        /// </summary>
        /// <param name="property">String of either the id, symbol or name of the cardinal.</param>
        /// <returns>The cardinal, if found. Null otherwise.</returns>
        public CC? CardinalFromProperties(string property)
        {
            if (int.TryParse(property, out int id))
            {
                if (Cardinals.TryGetValue(id, out CC? cardinal))
                {
                    return cardinal;
                }
            }
            CC? match = CardinalBySymbolOrNull(property);
            if (match != null)
            {
                return match;
            }
            return CardinalByNameOrNull(property);
        }
        /// <summary>
        /// Returns true if the input is n--m, where n and m are integers, giving the integer range of those numbers if so.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="intRange">All numbers from n to m (inclusive), or empty if m <= n.</param>
        /// <returns>True if string is of the form "n--m", where n and m are integers.</returns>
        public static bool TryStringIsIntRange(string input, out IEnumerable<int> intRange)
        {
            intRange = [];
            if (!input.Contains("--") || input.Split("--").Length != 2)
            {
                return false;
            }
            string[] splitInput = input.Split("--");
            if (int.TryParse(splitInput[0], out int lowerBound) && int.TryParse(splitInput[1], out int upperBound))
            {
                if (upperBound <= lowerBound)
                {
                    intRange = [];
                }
                else
                {
                    intRange = Enumerable.Range(lowerBound, upperBound - lowerBound + 1);
                }
                return true;
            }
            return false;
        }
        public (HashSet<int> CtoCItem1Ids, HashSet<int> CtoCItem2Ids, HashSet<int> MCNCardinalIds, HashSet<int> OtherCardinalIds) CardinalIdsSearch(string[] cardinalSearch)
        {
            throw new NotImplementedException();
        }
        public HashSet<int> ModelIdsSearch(string[] modelSearch)
        {
            throw new NotImplementedException();
        }
        public HashSet<char> TypesSearch(string[] typeSearch)
            { throw new NotImplementedException(); }
        public HashSet<int> ArticleIdsSearch(string[] articleSearch)
            { throw new NotImplementedException(); }
        public HashSet<int> TheoremIdsSearch(string[] theoremSearch)
            { throw new NotImplementedException(); }
        public HashSet<int> AgeSetSearch(string[] ageSearch)
            { throw new NotImplementedException(); }
        public HashSet<int> RelationIdsSearch(string[] idSearch)
            { throw new NotImplementedException(); }
    }
}