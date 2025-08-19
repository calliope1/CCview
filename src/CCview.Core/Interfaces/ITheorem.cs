using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;

namespace CCview.Core.Interfaces
{
    public interface ITheorem
    {
        int Id { get; }
        int ArtId { get; }
        string Description { get; }
        int GetArticleId();
        IArticle GetArticle();
        IEnumerable<ISentence> GetResults();
        string GetDescription();
        int GetId();
        bool AddResult<T>(T relType, int[] ids) where T : IRelationType;
    }
}