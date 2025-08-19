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
using CCview.Core.Interfaces;
using CCview.Core.JsonHandler.DataParsers;

namespace CCview.Core.DataClasses
{
    // "Atomic" relation that is proved directly by a single model or theorem
    // AtomicRelations (and Relations) then derive further Relations that have a witnessing signature of atomic relations
    public class AtomicRelation : IAtomicRelation
    {
        public Sentence Statement { get; private set; } = new();
        public ISentence GetStatement() => Statement;
        public IRelationType Type => Statement.Relationship;
        public IRelationType GetRelationType() => Statement.Relationship;
        public Theorem Witness { get; private set; } = new();
        public ITheorem GetWitness() => (ITheorem)Witness;
        public int WitnessId { get; private set; }
        public int GetWitnessId() => WitnessId;
        public int Item1Id => Statement.GetItem1();
        public int GetItem1Id() => Statement.GetItem1();
        public int Item2Id => Statement.GetItem2();
        public int GetItem2Id() => Statement.GetItem2();
        public int ModelId => Statement.GetModel();
        public int GetModelId() => Statement.GetModel();
        public int CardinalId => Statement.GetCardinal();
        public int GetCardinalId() => Statement.GetCardinal();
        public int Aleph => Statement.GetAleph();
        public int GetAleph() => Statement.GetAleph();
        public List<int> Ids => Statement.Ids;
        public IEnumerable<int> GetIds() => Statement.Ids;
        public AtomicRelation(Sentence statement, Theorem witness)
        {
            Statement = statement;
            Witness = witness;
            WitnessId = witness.Id;
        }
        public AtomicRelation(int witnessId, Sentence statement)
        {
            WitnessId = witnessId;
            Statement = statement;
        }
        public AtomicRelation(CC item1, CC item2, char type, Theorem witness)
        {
            Witness = witness;
            Statement = new(type, [item1.Id, item2.Id]);
        }
        public AtomicRelation() { }
        public void AddWitness(IReadOnlyDictionary<int, Theorem> theorems)
        {
            if (theorems.TryGetValue(WitnessId, out Theorem? witness))
            {
                Witness = witness;
            }
            else
            {
                throw new KeyNotFoundException($"Witness with ID {WitnessId} not found in theorems dictionary.");
            }
        }
        public override bool Equals(object? obj)
        {
            return obj is AtomicRelation other
                && other.Statement.Equals(Statement)
                && other.Witness.Equals(Witness);
        }
        public bool VerbEquals(object? obj)
        {
            if (obj is AtomicRelation other)
            {
                if (!Statement.VerbEquals(other.Statement))
                {
                    Console.WriteLine($"this.Statement ({this}.{Statement}) != other.Statement ({other}.{other.Statement})");
                    return false;
                }
                if (!Witness.VerbEquals(other.Witness))
                {
                    Console.WriteLine($"this.Witness ({this}.{Witness}) != other.Witness ({other}.{other.Witness})");
                    return false;
                }
                return true;
            }
            Console.WriteLine($"obj ({obj}) is not an AtomicRelation");
            return false;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Statement, Witness);
        }
        public override string ToString()
        {
            // This'll need to be fixed
            return $"Relation {Statement} from ID{Witness.Id}";
        }
        public string ToVerboseString(IRelationDatabase rd)
        {
            return $"{Statement.ToVerboseString(rd)} with witness {Witness}";
        }
    }
}