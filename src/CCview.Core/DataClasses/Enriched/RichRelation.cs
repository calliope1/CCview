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

namespace CCview.Core.DataClasses.Enriched
{
    /// <summary>
    /// A read-only relation that is enriched with subdata, such as the actual cardinal characteristics, not just their IDs.
    /// WORK IN PROGRESS: Not implemented yet.
    /// </summary>
    public class RichRelation : IRelation<RichRelation>
    {
        public int Id { get; private set; } = -1;
        public int GetId() { return Id; }
        public Sentence Statement { get; private set; } = new();
        public ISentence GetStatement() { return (ISentence)Statement; }
        public HashSet<AtomicRelation> Derivation { get; private set; } = [];
        public IEnumerable<IAtomicRelation> GetDerivation() { return Derivation.Cast<IAtomicRelation>(); }
        public RelationType Relationship => Statement.Relationship;
        public RelationType GetRelationship() { return Statement.Relationship; }
        public int Birthday => Derivation.Count > 0 ? Derivation.Select(a => a.Witness.Article.Date).Max() : int.MaxValue;
        public int GetBirthday() { return Birthday; }
        public List<int> Ids => Statement.Ids;
        public IEnumerable<int> GetIds() { return Ids; }
        //protected override List<string> FieldsToSave => ["Id", "Statement", "Derivation"];
        public int Item1Id => Statement.GetItem1();
        public int GetItem1Id() { return Item1Id; }
        public int Item2Id => Statement.GetItem2();
        public int GetItem2Id() { return Item2Id; }
        public int ModelId => Statement.GetModel();
        public int GetModelId() { return ModelId; }
        public int CardinalId => Statement.GetCardinal();
        public int GetCardinalId() { return CardinalId; }
        public int Aleph => Statement.GetAleph();
        public int GetAleph() { return Aleph; }
        public RichRelation() { throw new NotImplementedException("Enriched data structures not implemented yet."); }
        /// <summary>
        /// Constructor that copies the input relation.
        /// </summary>
        /// <param name="other">Relation to be copied.</param>
        public RichRelation(Relation other) : this()
        {
            Id = other.Id;
            Statement = other.Statement;
            Derivation = other.Derivation;
        }
        public RichRelation(Sentence statement, HashSet<AtomicRelation>[] derivations) : this()
        {
            Statement = statement;
            Derivation = [];
            foreach (HashSet<AtomicRelation> der in derivations)
            {
                Derivation.UnionWith(der);
            }
        }
        public RichRelation(int id, Sentence statement, HashSet<AtomicRelation> derivation) : this()
        {
            Id = id;
            Statement = statement;
            Derivation = derivation;
        }
        public RichRelation(Sentence statement, HashSet<AtomicRelation> derivation) : this(statement, [derivation]) { }
        public RichRelation(AtomicRelation atom, int id) : this()
        {
            Id = id;
            Statement = atom.Statement;
            Derivation = [atom];
        }

        public override bool Equals(object? obj) // This needs to be overhauled because of 'signature's.
        {
            return obj is Relation other
                && Statement.Equals(other.Statement)
                && Derivation.SetEquals(other.Derivation);
        }
        public bool VerbEquals(object? obj)
        {
            if (obj is Relation other)
            {
                if (!Statement.VerbEquals(other.Statement))
                {
                    Console.WriteLine($"this.Statement ({this}.{Statement}) != other.Statement ({other}.{other.Statement})");
                    return false;
                }
                if (!Derivation.SetEquals(other.Derivation))
                {
                    Console.WriteLine($"this.Derivation ({this}.{Derivation}) != other.Derivation ({other}.{other.Derivation})");
                    return false;
                }
                return true;
            }
            Console.WriteLine($"obj ({obj}) is not a Relation");
            return false;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Statement, Derivation); // Needs overhauling
        }
        public override string ToString()
        {
            string outString = $"(Proof length {Derivation.Count}, ID{Id})";
            outString = Relationship.GetFamily() switch
            {
                "CtoC" => $"Relation ID{Item1Id} {Relationship} ID{Item2Id} {outString}",
                "MCN" => $"Relation ID{ModelId} models ID{CardinalId} {Relationship} Aleph_{Aleph} {outString}",
                _ => $"Relation type {Relationship} with statement {Statement} {outString}",
            };
            return outString;
        }
        public string ToStringWithSymbols<T>(IReadOnlyDictionary<int, T> cardinals) where T : ICardinalCharacteristic
        {
            return Relationship.GetFamily() switch
            {
                "CtoC" => $"Relation {cardinals[Item1Id].GetEquationSymbol()} (ID{Item1Id}) {Relationship} {cardinals[Item2Id].GetEquationSymbol()} (ID{Item2Id}); {Derivation.Count} length proof, ID{Id}",
                "MCN" => $"Relation ID{ModelId} models {cardinals[CardinalId].GetEquationSymbol()} (ID{CardinalId}) {Relationship} Aleph_{Aleph} ({Derivation.Count} length proof, ID{Id})",
                _ => $"Relation type {Relationship} with statement {Statement} ({Derivation.Count} length proof, ID{Id})",
            };
        }
        public string ToVerboseString(RelationDatabase rd)
        {
            string returnString = $"Relation ID {Id}\n{Statement.ToVerboseString(rd)}";
            if (Ids.Count == 0)
            {
                returnString += "\nNo derivation for this relation (that shouldn't happen...).";
            }
            else
            {
                returnString += $"\nDerivation is length {Ids.Count}:";
                foreach (AtomicRelation atomicRelation in Derivation)
                {
                    returnString += $"\n{atomicRelation.ToVerboseString(rd)}";
                }
            }
            return returnString;
        }
        public bool ResultEquals(RichRelation other)
        {
            return Statement.Equals(other.GetStatement());
        }
        public RichRelation? Deduce(RichRelation other)
        {
            if (Relationship.Symbol == 'X' || other.Relationship.Symbol == 'X')
            {
                return null;
            }
            // Potential improvement:
            // Sentence? newStatement;
            // Then instead of 'return new(new(...), [Derivation, other.Derivation]);' we have 'newStatement = new(...); break;'
            // Then at the end we return new(newStatement, [Derivation,other.Derivation]. Cleaner looking code.
            switch (Relationship.Symbol, other.Relationship.Symbol)
            {
                case ('>', '>'):
                    if (Ids[1] == other.Ids[0])
                    {
                        if (Ids[0] == other.Ids[1])
                        {
                            return new(new('=', [Ids[0], Ids[1]]), [Derivation, other.Derivation]);
                        }
                        return new(new('>', [Ids[0], other.Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[0] == other.Ids[1])
                    {
                        return new(new('>', [other.Ids[0], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('>', 'C'):
                    if (other.Ids[1] == Ids[0])
                    {
                        return new(new('C', [other.Ids[1], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('>', 'V'):
                    if (Ids[1] == other.Ids[1])
                    {
                        return new(new('G', [other.Ids[0], Ids[0], other.Ids[2]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[0] == other.Ids[1])
                    {
                        return new(new('L', [other.Ids[0], Ids[1], other.Ids[2]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('>', 'G'):
                    if (Ids[1] == other.Ids[1])
                    {
                        return new(new('G', [other.Ids[0], Ids[0], other.Ids[2]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('>', 'L'):
                    if (Ids[0] == other.Ids[1])
                    {
                        return new(new('L', [other.Ids[0], Ids[1], other.Ids[2]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('>', '='):
                    // We can probably just write 'if (Ids.SetEquals(other.Ids))', but that will require further investigation
                    HashSet<int> thisIdsGE = [Ids[0], Ids[1]];
                    HashSet<int> otherIdsGE = [other.Ids[0], other.Ids[1]];
                    if (thisIdsGE.SetEquals(otherIdsGE))
                    {
                        return null;
                    }
                    if (Ids[0] == other.Ids[0])
                    {
                        return new(new('>', [other.Ids[1], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[0] == other.Ids[1])
                    {
                        return new(new('>', [other.Ids[0], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[0])
                    {
                        return new(new('>', [Ids[0], other.Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[1])
                    {
                        return new(new('>', [Ids[0], other.Ids[0]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('C', '>'):
                    return other.Deduce(this);
                case ('C', 'C'):
                    return null;
                case ('C', 'X'):
                    return null;
                case ('C', 'V'):
                    return null;
                case ('C', 'G'):
                    return null;
                case ('C', 'L'):
                    return null;
                case ('C', '='):
                    if (Ids[0] == other.Ids[0])
                    {
                        return new(new('C', [other.Ids[1], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[0] == other.Ids[1])
                    {
                        return new(new('C', [other.Ids[0], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[0])
                    {
                        return new(new('C', [Ids[0], other.Ids[1]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[1])
                    {
                        return new(new('C', [Ids[0], other.Ids[0]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('V', '>'):
                    return other.Deduce(this);
                case ('V', 'C'):
                    return other.Deduce(this);
                case ('V', 'V'):
                    if (Ids[0] == other.Ids[0]
                        && Ids[1] != other.Ids[1]
                        && Ids[2] != other.Ids[2])
                    {
                        if (Ids[2] < other.Ids[2])
                        {
                            return new(new('C', [other.Ids[1], Ids[1]]), [Derivation, other.Derivation]);
                        }
                        else
                        {
                            return new(new('C', [Ids[1], other.Ids[1]]), [Derivation, other.Derivation]);
                        }
                    }
                    return null;
                case ('V', 'G'):
                    if (Ids[0] == other.Ids[0]
                        && Ids[1] != other.Ids[1]
                        && Ids[2] < other.Ids[2])
                    {
                        return new(new('C', [other.Ids[1], Ids[1]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('V', 'L'):
                    if (Ids[0] == other.Ids[0]
                        && Ids[1] != other.Ids[1]
                        && Ids[2] > other.Ids[2])
                    {
                        return new(new('C', [Ids[1], other.Ids[1]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('V', '='):
                    if (Ids[1] == other.Ids[0])
                    {
                        return new(new('V', [Ids[0], other.Ids[1], Ids[2]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[1])
                    {
                        return new(new('V', [Ids[0], other.Ids[0], Ids[2]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('G', '>'):
                    return other.Deduce(this);
                case ('G', 'C'):
                    return other.Deduce(this);
                case ('G', 'V'):
                    return other.Deduce(this);
                case ('G', 'G'):
                    return null;
                case ('G', 'L'):
                    // There's probably a pithy way to say this, but I don't want to risk it for the moment.
                    if (Ids[0] == other.Ids[0] && Ids[1] == other.Ids[1] && Ids[2] == other.Ids[2])
                    {
                        return new(new('V', Ids), [Derivation, other.Derivation]);
                    }
                    // You could include a 'watch out for contradictory data' check here
                    return null;
                case ('G', '='):
                    if (Ids[1] == other.Ids[0])
                    {
                        return new(new('G', [Ids[0], other.Ids[1], Ids[2]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[1])
                    {
                        return new(new('G', [Ids[0], other.Ids[0], Ids[2]]), [Derivation, other.Derivation]);
                    }
                    return null;
                case ('L', '>'):
                    return other.Deduce(this);
                case ('L', 'C'):
                    return other.Deduce(this);
                case ('L', 'V'):
                    return other.Deduce(this);
                case ('L', 'G'):
                    return other.Deduce(this);
                case ('L', 'L'):
                    return null;
                case ('L', '='):
                    if (Ids[1] == other.Ids[0])
                    {
                        return new(new('L', [Ids[0], other.Ids[1], Ids[2]]), [Derivation, other.Derivation]);
                    }
                    else if (Ids[1] == other.Ids[1])
                    {
                        return new(new('L', [Ids[0], other.Ids[0], Ids[2]]), [Derivation, other.Derivation.ToHashSet()]);
                    }
                    return null;
                case ('=', '>'):
                    return other.Deduce(this);
                case ('=', 'C'):
                    return other.Deduce(this);
                case ('=', 'V'):
                    return other.Deduce(this);
                case ('=', 'G'):
                    return other.Deduce(this);
                case ('=', 'L'):
                    return other.Deduce(this);
                case ('=', '='):
                    HashSet<int> allIdsEE = [.. Ids.Union(other.Ids)];
                    if (allIdsEE.Count != 3)
                    {
                        return null;
                    }
                    List<(int, int)> twoTimesTwo = [(0, 0), (0, 1), (1, 0), (1, 1)];
                    foreach ((int i, int j) in twoTimesTwo)
                    {
                        if (Ids[i] == other.Ids[j])
                        {
                            return new(new('=', [Ids[1 - i], other.Ids[1 - j]]), [Derivation, other.Derivation]);
                        }
                    }
                    throw new ArgumentException($"this.Ids ({Ids}) \\cup other.Ids ({other.Ids}) == 3, but this.Ids \\cap other.Ids == \\emptyset...");
                default:
                    throw new NotImplementedException($"Relation {this} has unexpected type {Relationship}.");
            }
        }
    }
}