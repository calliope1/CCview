using CCview.Core.DataClasses;
using CCview.Core.GraphLogic;
using CCview.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using ICC = CCview.Core.Interfaces.ICardinalCharacteristic;

namespace CCview.Core.Interfaces
{
    public interface IRelationDatabase
    {
        IReadOnlyDictionary<int, CC> GetCardinals();
        HashSet<AtomicRelation> GetAtomicRelations();
        IReadOnlyDictionary<int, Relation> GetRelations();
        //private Dictionary<Sentence, (Relation Oldest, Relation Shortest)> OldestAndShortestRelations { get; set; } = [];
        IReadOnlyDictionary<int, Article> GetArticles();
        IReadOnlyDictionary<int, Theorem> GetTheorems();
        IReadOnlyDictionary<int, Model> GetModels();
        IReadOnlyDictionary<(int LargerId, int SmallerId), HashSet<int>> GetDensity();
        bool DynamicDensity { get; }
        bool PopulateDensity(bool overrideChecks = false, bool fromDeductiveClosure = false);
        int CreateTrivialRelations(bool overrideCheck = false);
        int LogicTransClose();
        Relation AddCtoCRelation(CC? a, CC? b, char type, Theorem? witness);
        Relation AddMCNRelation(Model? m, CC? c, int n, char type, Theorem? witness);
        static int NewDictId<T>(IReadOnlyDictionary<int, T> dict, bool fast = false)
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
        Article AddArticle(Article article);
        Article AddArticle(string? name, int date, string? citation, int id);
        Article AddArticle(string? name, int date, string? citation)
        {
            int newId = NewDictId(GetArticles());
            return AddArticle(name, date, citation, newId);
        }
        CC AddCardinal(string? name, string? symbol, int id);
        CC AddCardinal(string? name, string? symbol, bool fast = false)
        {
            return AddCardinal(name, symbol, NewDictId(GetCardinals(), fast));
        }
        Theorem AddTheorem(Article article, string description, HashSet<Sentence> results, int id);
        Theorem AddTheorem(Article article, string description, HashSet<Sentence> results)
        {
            int newId = NewDictId(GetTheorems());
            return AddTheorem(article, description, results, newId);
        }
        static bool AddResultToTheorem(Theorem theorem, RelationType type, int[] ids)
        {
            return theorem.AddResult(type, ids);
        }
        Model AddModel(Article article, string? description, int id);
        Model AddModel(Article article, string description)
        {
            int newId = NewDictId(GetModels());
            return AddModel(article, description, newId);
        }
        HashSet<Relation> GetMinimalRelations(IReadOnlyDictionary<int, CC> desiredCardinals);
        static T? GetTByIdOrDefault<T>(IReadOnlyDictionary<int, T> dict, int id, T? defaultValue)
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
        CC? GetCardinalById(int id)
        {
            return GetTByIdOrDefault(GetCardinals(), id, default);
        }

        Relation? GetRelationById(int id)
        {
            return GetTByIdOrDefault(GetRelations(), id, default);
        }
        Article? GetArticleById(int id)
        {
            return GetTByIdOrDefault(GetArticles(), id, default);
        }
        Theorem? GetTheoremById(int id)
        {
            return GetTByIdOrDefault(GetTheorems(), id, default);
        }
        Model? GetModelById(int id)
        {
            return GetTByIdOrDefault(GetModels(), id, default);
        }
        bool AddCtoCRelationByIds(int id1, int id2, char type, int witnessId);
        bool AddMCNRelationByIds(int modId, int ccId, int n, char type, int witnessId);
        CC? CardinalBySymbolOrNull(string symbol);
        CC GetCardinalBySymbol(string symbol);
        CC? CardinalByNameOrNull(string name);
        CC GetCardinalByName(string name);
        int GenerateAtoms();
        /// <summary>
        /// Get a cardinal from one of its properties, checking (in order): ID number, symbol, and name.
        /// </summary>
        /// <param name="property">String of either the id, symbol or name of the cardinal.</param>
        /// <returns>The cardinal, if found. Null otherwise.</returns>
        CC? CardinalFromProperties(string property);
        /// <summary>
        /// Returns true if the input is n--m, where n and m are integers, giving the integer range of those numbers if so.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="intRange">All numbers from n to m (inclusive), or empty if m <= n.</param>
        /// <returns>True if string is of the form "n--m", where n and m are integers.</returns>
        static bool TryStringIsIntRange(string input, out IEnumerable<int> intRange)
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
        (HashSet<int> CtoCItem1Ids, HashSet<int> CtoCItem2Ids, HashSet<int> MCNCardinalIds, HashSet<int> OtherCardinalIds) CardinalIdsSearch(string[] cardinalSearch);
        HashSet<int> ModelIdsSearch(string[] modelSearch);
        HashSet<char> TypesSearch(string[] typeSearch);
        HashSet<int> ArticleIdsSearch(string[] articleSearch);
        HashSet<int> TheoremIdsSearch(string[] theoremSearch);
        HashSet<int> AgeSetSearch(string[] ageSearch);
        HashSet<int> RelationIdsSearch(string[] idSearch);
    }
}
