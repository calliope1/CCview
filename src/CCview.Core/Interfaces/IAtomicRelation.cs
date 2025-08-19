using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Services;
using System.Collections.Generic;

namespace CCview.Core.Interfaces
{
    // "Atomic" relation that is proved directly by a single model or theorem
    // AtomicRelations (and Relations) then derive further Relations that have a witnessing signature of atomic relations
    public interface IAtomicRelation
    {
        ISentence GetStatement();
        IRelationType Type { get; }
        IRelationType GetRelationType();
        ITheorem GetWitness();
        int GetWitnessId();
        int GetItem1Id();
        int GetItem2Id();
        int GetModelId();
        int GetCardinalId();
        int GetAleph();
        IEnumerable<int> GetIds();
    }
}