using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Interfaces;

namespace CCview.Core.DataClasses
{
    /// <summary>
    /// "Atomic" unit of proof, one theorem that proves the provided sentence.
    /// </summary>
    public class Theorem : ITheorem
    {
        public int Id { get; private set; } = -1;
        public int GetId() => Id;
        public int ArtId { get; private set; } = -1;
        public int GetArticleId() => ArtId;
        public Article Article { get; private set; } = null!;
        public IArticle GetArticle() => (IArticle)Article;
        public HashSet<Sentence> Results { get; set; } = [];
        public IEnumerable<ISentence> GetResults() => Results.Cast<ISentence>();
        public string Description { get; private set; } = "No description provided.";
        public string GetDescription() => Description;
        public Theorem(int id, Article article, HashSet<Sentence> results, string description)
        {
            Id = id;
            Article = article;
            Results = results;
            Description = description;
            ArtId = Article.Id;
        }
        public Theorem(int id, int articleId, string description, HashSet<Sentence> results)
        {
            Id = id;
            ArtId = articleId;
            Results = results;
            Description = description;
        }
        public Theorem() { }
        public bool SetArticleByDictionary(IReadOnlyDictionary<int, Article> articles)
        {
            if (articles.TryGetValue(ArtId, out Article? article))
            {
                if (article.Equals(Article))
                {
                    return false;
                }
                Article = article;
                return true;
            }
            throw new ArgumentException($"Article with ID {ArtId} not found in the provided dictionary.");
        }
        public override bool Equals(object? obj)
        {
            return obj is Theorem other && Id.Equals(other.Id);
        }
        /// <summary>
        /// Verbose equality. Same output as <see cref="Equals"/> but writes reasoning to the console.
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns></returns>
        public bool VerbEquals(object? obj)
        {
            if (obj is Theorem other)
            {
                if (!Id.Equals(other.Id))
                {
                    Console.WriteLine($"this.Id ({this}.{Id}) != other.Id ({other}.{other.Id})");
                    return false;
                }
                return true;
            }
            Console.WriteLine($"obj ({obj}) is not a Theorem");
            return false;
        }
        /// <summary>
        /// Adds the Sentence with relationship <paramref name="relType"/> and ids <paramref name="ids"/>
        /// to the theorem.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="relType"></param>
        /// <param name="ids"></param>
        /// <returns>
        /// True if and only if the result was not already in <c>Results</c>.
        /// </returns>
        public bool AddResult<T>(T relType, int[] ids) where T : IRelationType
        {
            Sentence newResult = new(relType.GetSymbol(), [.. ids]);
            // TestIdsLength is already called by the prior constructor, so this is more for safety.
            newResult.TestIdsLength();
            if (Results.Contains(newResult))
            {
                return false;
            }
            Results.Add(newResult);
            return true;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Theorem ID:{Id} '{Description}' of {Article}";
        }
    }
}