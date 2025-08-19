using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using ICC = CCview.Core.Interfaces.ICardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Services;

namespace CCview.Core.Interfaces
{
    public interface IRelation<T> where T : IRelation<T>
    {
        int Id { get; }
        int GetId();
        ISentence GetStatement();
        IEnumerable<IAtomicRelation> GetDerivation();
        RelationType GetRelationship();
        int GetBirthday();
        IEnumerable<int> GetIds();
        int GetItem1Id();
        int GetItem2Id();
        int GetModelId();
        int GetCardinalId();
        int GetAleph();
        string ToStringWithSymbols<S>(IReadOnlyDictionary<int, S> cardinals) where S : ICC;
        bool ResultEquals(T other);
        T? Deduce(T other);
    }
}