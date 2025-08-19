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

namespace CCview.Core.DataClasses
{
    // The "Sentence" is the most fundamental statement about cardinal characteristics
    // It is not saved, but it serves as the way of understanding how two cardinals are being related
    public class Sentence : ISentence
    {
        public RelationType Relationship { get; private set; }
        public RelationType GetRelationType() => Relationship;
        public List<int> Ids { get; private set; } = [];
        public IEnumerable<int> GetIds() => Ids;
        public Sentence() { Relationship = new('X'); }
        public Sentence(char type, List<int> ids)
        {
            Relationship = new(type);
            Ids = ids;
            TestIdsLength();
        }
        public Sentence(int relationIndex, List<int> ids)
        {
            Relationship = new(relationIndex);
            Ids = ids;
            TestIdsLength();
        }
        public bool TestIdsLength()
        {
            switch (Relationship.GetFamily())
            {
                case "CtoC":
                    if (Ids.Count != 2)
                    {
                        throw new ArgumentException($"Ids for a '{Relationship}' type sentence must be two Ids of cardinal characteristics.");
                    }
                    break;
                case "MCN":
                    if (Ids.Count != 3)
                    {
                        throw new ArgumentException($"Ids for a '{Relationship}' type sentence must be one model id, one cardinal characteristic id and one positive number.");
                    }
                    break;
                case "None":
                    Console.WriteLine("Warning: You have instantiated a new type 'X' sentence.");
                    break;
                default:
                    throw new NotImplementedException($"Relation type {Relationship} has not been implemented.");
            }
            return true;
        }
        public override bool Equals(object? obj)
        {
            return obj is Sentence other
                && Relationship.Equals(other.Relationship)
                && Ids.SequenceEqual(other.Ids);
        }
        public bool VerbEquals(object? obj)
        {
            if (obj is Sentence other)
            {
                if (Relationship != other.Relationship)
                {
                    Console.WriteLine($"this.Type ({this}.{Relationship}) != other.Type ({other}.{other.Relationship})");
                    return false;
                }
                if (!Ids.SequenceEqual(other.Ids))
                {
                    Console.WriteLine($"Enumerable.SequenceEqual(this.Ids ({this}.{Ids}), other.Ids({other}.{other.Ids}) = false");
                    return false;
                }
                return true;
            }
            Console.WriteLine($"obj ({obj}) is not a Sentence");
            return false;
        }
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Relationship);
            foreach (int id in Ids)
            {
                hash.Add(id);
            }
            return hash.ToHashCode();
        }
        public override string ToString()
        {
            string toString = $"Sentence {Relationship} for ids [";
            if (Ids.Count > 0)
            {
                foreach (int id in Ids)
                {
                    toString += id.ToString() + ", ";
                }
                // We definitely have at least two characters, but lets be safe
                toString = toString[..Math.Max(0, toString.Length - 2)];
                toString += "]";
            }
            return toString;
        }
        public string ToVerboseString(IRelationDatabase rd)
        {
            string returnString = "";
            switch (Relationship.GetFamily())
            {
                case "CtoC":
                    returnString += $"{rd.GetCardinalById(GetItem1())} {Relationship} {rd.GetCardinalById(GetItem2())}";
                    break;
                case "MCN":
                    returnString += $"{rd.GetModelById(GetModel())} models {rd.GetCardinalById(GetCardinal())} {Relationship} Aleph_{GetAleph()}";
                    break;
                default:
                    if (Ids.Count == 0)
                    {
                        returnString += $"Unanticipated type {Relationship} relation with no ids";
                    }
                    else
                    {
                        returnString += $"Unanticipated type {Relationship} relation with ids [";
                        foreach (int i in Ids)
                        {
                            returnString += $"{i}, ";
                        }
                        returnString = returnString[..(returnString.Length - 2)];
                        returnString += "]";
                    }
                    break;
            }
            return returnString;
        }
        public int GetItem1()
        {
            if (Relationship.GetFamily().Equals("CtoC")) return Ids[0];
            else throw new ArgumentException($"Type {Relationship} Sentences do not have an Item1.");
        }
        public int GetItem2()
        {
            if (Relationship.GetFamily().Equals("CtoC")) return Ids[1];
            else throw new ArgumentException($"Type {Relationship} Sentences do not have an Item2.");
        }
        public int GetModel()
        {
            if (Relationship.GetFamily().Equals("MCN")) return Ids[0];
            else throw new ArgumentException($"Type {Relationship} Sentences do not have a Model.");
        }
        public int GetCardinal()
        {
            if (Relationship.GetFamily().Equals("MCN")) return Ids[1];
            else throw new ArgumentException($"Type {Relationship} Sentences do not have a CC. Did you mean GetItem1 or GetItem2?");
        }
        public int GetAleph()
        {
            if (Relationship.GetFamily() == "MCN") return Ids[2];
            else throw new ArgumentException($"Type {Relationship} Sentences do not have an Aleph.");
        }
    }
}