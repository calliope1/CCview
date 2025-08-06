using Newtonsoft.Json;
using CCView.GraphLogic.Algorithms;
using CC = CCView.CardinalData.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCView.JsonHandler;
using System.Linq;
using CCView.CardinalData.Compute;

namespace CCView.CardinalData
{
    public class CardinalCharacteristic : JsonCRTP<CardinalCharacteristic>
    {
        //public int Id { get; private set; } = -1;
        public string Name { get; set; } = "No name assigned";
        public string SymbolString { get; set; } = "X";
        protected override List<string> FieldsToSave => ["Id", "Name", "SymbolString"];

        [JsonConstructor] // Telling Json.NET to use this constructor. Probably redundant
        public CardinalCharacteristic(JArray args)
        {
            Id = args[0].Value<int>();
            Name = args[1].Value<string>() ?? "No name assigned.";
            SymbolString = args[2].Value<string>() ?? "X";
        }
        public override void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            Name = args[1].Value<string>() ?? "No name assigned.";
            SymbolString = args[2].Value<string>() ?? "X";
        }
        public CardinalCharacteristic(int id, string name, string symbolString)
        {
            Id = id;
            Name = name;
            SymbolString = symbolString;
        }
        public CardinalCharacteristic() { }

        public override bool Equals(object? obj)
        {
            return obj is CardinalCharacteristic other && this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id})";
        }
    }
    public class Article : JsonCRTP<Article>
    {
        //public int Id { get; private set; } = -1;
        // Generally represented as YYYYMMDD, with XX = 99 if not found. MaxValue for 'no year' for simplicity
        public int Date { get; private set; } = int.MaxValue;
        public string Name { get; private set; } = "Article name required!";
        public string Citation { get; private set; } = "Citation required!";
        // We're not going to save the subordinate Theorems, since these can be reconstructed at runtime
        protected override List<string> FieldsToSave => ["Id", "Date", "Name", "Citation"];
        public Article(int id, int date, string name, string citation)
        {
            Id = id;
            Date = date;
            Name = name;
            Citation = citation;
        }
        public Article() { }
        public override void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            Date = args[1].Value<int>();
            Name = args[2].Value<string>() ?? "Article name required!";
            Citation = args[3].Value<string>() ?? "Citation required!";
        }
        public override bool Equals(object? obj)
        {
            return obj is Article other && other.Id == Id;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Article {Name} (ID: {Id}) from {Date}";
        }
        public void GetNewId(RelationDatabase rD, bool fast = false)
        {
            this.Id = RelationDatabase.NewDictId<Article>(rD.Articles, fast);
        }
    }
    // "Atomic" unit of proof, one theorem or model that implies one or more relations directly
    public class Theorem : JsonCRTP<Theorem>
    {
        public int ArtId { get; set; } = -1;
        public Article Article { get; set; } = null!;
        // Results is directly proved relations between cardinals in the description
        // If there is a model that implicitly proves certain relations, use the Model subclass
        public HashSet<Sentence> Results { get; set; } = [];
        public string Description { get; set; } = "No description provided.";
        protected override List<string> FieldsToSave => ["Id", "ArtId", "Description", "Results"];
        public Theorem(int id, Article article, HashSet<Sentence> results, string description)
        {
            Id = id;
            Article = article;
            Results = results;
            Description = description;
            ArtId = Article.Id;
        }
        public Theorem() { }
        public override void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            ArtId = args[1].Value<int>();
            Description = args[2].Value<string>() ?? "No description provided.";
            foreach (JArray jArray in args[3].Cast<JArray>())
            {
                Results.Add(Sentence.FromJArray(jArray));
            }
        }
        public override bool Equals(object? obj)
        {
            return obj is Theorem other && Id.Equals(other.Id);
        }
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
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Theorem ID:{Id} '{Description}' of {Article}";
        }
    }
    public class Model : JsonCRTP<Model>
    {
        public Article Article { get; set; } = new();
        public int ArtId { get; set; } = -1;
        public string Description { get; set; } = "No description provided.";
        public HashSet<(CC Cardinal, int Aleph, Theorem Witness)> Values { get; set; } = [];
        public HashSet<IntThree> ValIds { get; set; } = [];
        protected override List<string> FieldsToSave => ["Id", "Article.Id", "ResIds", "Description", "ValIds"];
        private readonly int Cid = Sentence.TypeIdFromChar('C');
        public Model(int id, Article article, string description, HashSet<(CC Cardinal, int Aleph, Theorem Witness)> values)
        {
            Id = id;
            Description = description;
            Article = article;
            Values = values;
            ArtId = article.Id;
            foreach ((CC Cardinal, int Aleph, Theorem Witness) tup in Values)
            {
                ValIds.Add(new(tup.Cardinal.Id, tup.Aleph, tup.Witness.Id));
            }
        }
        public Model() { }
        public override void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            ArtId = args[1].Value<int>();
            Description = args[3].Value<string>() ?? "No description provided!";
            foreach (JArray alephArray in args[4].Cast<JArray>())
            {
                List<int> newList = alephArray.Value<List<int>>()!;
                ValIds.Add(new(newList));
            }
        }
        public override bool Equals(object? obj)
        {
            return obj is Model other && Id.Equals(other.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Model ID:{Id} '{Description}' from {Article}";
        }
    }
    // The "Sentence" is the most fundamental statement about cardinal characteristics
    // It is not saved, but it serves as the way of understanding how two cardinals are being related
    public class Sentence : JsonToArray
    {
        public char Type { get; set; } = 'X';
        public List<int> Ids { get; set; } = [];
        /// <summary>
        /// Type, Ids:
        /// >, [x.Id, y.Id] means 'CC x, CC y, and ZFC proves x \geq y'
        /// C, [x.Id, y.Id] means 'CC x, CC y, and Con(ZFC + x > y)'
        /// X, [any] means 'You forgot to assign a Type'
        /// V, [m.Id, x.Id, n] means 'CC x, Model m, and m \models x = \aleph_n'
        /// G, [m.Id, x.Id, n] means 'CC x, Model m, and m \models x \geq \aleph_n'
        /// L, [m.Id, x.Id, n] means 'CC x, Model m, and m \models x \leq \aleph_n'
        /// =, [x.Id, y.Id] means 'CC x, CC y, and ZFC proves x = y'
        /// </summary>
        public static List<char> TypeIndices { get; } = ['>', 'C', 'X', 'V', 'G', 'L', '='];
        public static List<char> CtoCTypes { get; } = ['>', 'C', '='];
        public static List<char> MCNTypes { get; } = ['V', 'G', 'L'];
        public Sentence() { }
        public Sentence(char type, List<int> ids)
        {
            Type = type;
            Ids = ids;
            List<char> anticipatedTypes = CtoCTypes.Union(MCNTypes).ToList();
            anticipatedTypes.Add('X');
            if (!anticipatedTypes.Contains(Type))
            {
                throw new NotImplementedException($"Sentence {this} has unanticipated type {Type}.");
            }
            if (CtoCTypes.Contains(Type) && Ids.Count != 2)
            {
                throw new ArgumentException($"Ids for a '{Type}' type sentence must be two Ids of cardinal characteristics.");
            }
            if (MCNTypes.Contains(Type) && Ids.Count != 3)
            {
                throw new ArgumentException($"Ids for a '{Type}' type sentence must be one model id, one cardinal characteristic id and one positive number.");
            }
            if (Type.Equals('X'))
            {
                Console.WriteLine("Warning: You have instantiated a new type 'X' sentence.");
            }
        }
        public Sentence(CC item1, CC item2, char type) : this(type, [item1.Id, item2.Id]) { }
        public override JArray TurnToJson()
        {
            JArray jArray = [];
            JToken TypeIdJTok = JToken.FromObject(TypeIdFromChar(Type));
            jArray.Add(TypeIdJTok);
            JArray idsArray = JArray.FromObject(Ids);
            jArray.Add(idsArray);
            //JArray subArray = [];
            //foreach (int i in Ids)
            //{
            //    subArray.Add(JObject.FromObject(i));
            //}
            //jArray.Add(subArray);
            return jArray;
        }
        public static Sentence FromJArray(JArray args)
        {
            List<int> newIds = args[1].Select(t => t.Value<int>()).ToList();
            //List<int> newIds = [];
            //foreach (JToken arg in args[1])
            //{
            //    newIds.Add(arg.Value<int>());
            //}
            return new(Sentence.TypeIndices[args[0].Value<int>()], newIds);
        }
        public override bool Equals(object? obj)
        {
            return obj is Sentence other
                && Type == other.Type
                && Enumerable.SequenceEqual(Ids, other.Ids);
        }
        public bool VerbEquals(object? obj)
        {
            if (obj is Sentence other)
            {
                if (Type != other.Type)
                {
                    Console.WriteLine($"this.Type ({this}.{Type}) != other.Type ({other}.{other.Type})");
                    return false;
                }
                if (!Enumerable.SequenceEqual(Ids, other.Ids))
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
            hash.Add(Type);
            foreach (int id in Ids)
            {
                hash.Add(id);
            }
            return hash.ToHashCode();
        }
        public override string ToString()
        {
            string toString = $"Sentence {Type} for ids [";
            if (Ids.Count > 0)
            {
                foreach (int id in Ids)
                {
                    toString += id.ToString() + ", ";
                }
                // We definitely have at least two characters, but lets be safe
                toString = toString.Substring(0, Math.Max(0, toString.Length - 2));
            }
            return toString;
        }
        // TypeIds *could* be shorts because char and short are both 16 bit, but this is excessive
        public static int TypeIdFromChar(Char type)
        {
            return TypeIndices.IndexOf(type);
        }
        public int GetItem1()
        {
            if (CtoCTypes.Contains(Type)) return Ids[0];
            else throw new ArgumentException($"Type {Type} Sentences do not have an Item1.");
        }
        public int GetItem2()
        {
            if (CtoCTypes.Contains(Type)) return Ids[1];
            else throw new ArgumentException($"Type {Type} Sentences do not have an Item2.");
        }
        public int GetModel()
        {
            if (MCNTypes.Contains(Type)) return Ids[0];
            else throw new ArgumentException($"Type {Type} Sentences do not have a Model.");
        }
        public int GetCC()
        {
            if (MCNTypes.Contains(Type)) return Ids[1];
            else throw new ArgumentException($"Type {Type} Sentences do not have a CC. Did you mean GetItem1 or GetItem2?");
        }
        public int GetAleph()
        {
            if (MCNTypes.Contains(Type)) return Ids[2];
            else throw new ArgumentException($"Type {Type} Sentences do not have an Aleph.");
        }
    }
    // "Atomic" relation that is proved directly by a single model or theorem
    // AtomicRelations (and Relations) then derive further Relations that have a witnessing signature of atomic relations
    public class AtomicRelation : JsonToArray
    {
        public Sentence Statement { get; set; } = new();
        public Char Type => Statement.Type;
        public Theorem Witness { get; set; } = new();
        public int WitnessId { get; set; }
        public AtomicRelation(Sentence statement, Theorem witness)
        {
            Statement = statement;
            Witness = witness;
            WitnessId = witness.Id;
        }
        public AtomicRelation(CC item1, CC item2, char type, Theorem witness)
        {
            Witness = witness;
            Statement = new(type, [item1.Id, item2.Id]);
        }
        public AtomicRelation() { }
        public override JArray TurnToJson()
        {
            JArray jArray = [JToken.FromObject(Witness.Id)];
            jArray.Add(Statement.TurnToJson());
            return jArray;
        }
        public static AtomicRelation FromJArray(JArray args)
        {
            AtomicRelation r = new();
            r.WitnessId = args[0].Value<int>();
            r.Statement = Sentence.FromJArray((JArray)args[1]);
            return r;
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
    }
    public class Relation : JsonCRTP<Relation>
    {
        public Sentence Statement { get; set; } = new();
        public HashSet<AtomicRelation> Derivation { get; set; } = [];
        public char Type => Statement.Type;
        public int Age => Derivation.Count > 0 ? Derivation.Select(a => a.Witness.Article.Date).Max() : int.MaxValue;
        public List<int> Ids => Statement.Ids;
        protected override List<string> FieldsToSave => ["Statement", "Derivation"];
        public Relation() { }
        public Relation(Sentence statement, HashSet<AtomicRelation>[] derivations)
        {
            Statement = statement;
            Derivation = [];
            foreach (HashSet<AtomicRelation> der in derivations)
            {
                Derivation.UnionWith(der);
            }
        }
        public Relation(Sentence statement, HashSet<AtomicRelation> derivation) : this(statement, [derivation]) { }
        public override void InstantiateFromJArray(JArray args)
        {
            Statement = Sentence.FromJArray((JArray)args[0]);
            foreach (JArray jArray in args[1].Cast<JArray>())
            {
                Derivation.Add(AtomicRelation.FromJArray(jArray));
            }
        }
        public Relation(AtomicRelation atom)
        {
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
            if (Sentence.CtoCTypes.Contains(Statement.Type))
            {
                return $"Relation ID{Statement.GetItem1()} {Type} ID{Statement.GetItem2()} ({Derivation.Count} length proof)";
            }
            else if (Sentence.MCNTypes.Contains(Statement.Type))
            {
                return $"Relation ID{Statement.GetModel()} models ID{Statement.GetCC} {Type} ID{Statement.GetAleph()} ({Derivation.Count} length proof)";
            }
            else
            {
                return $"Relation {Statement}";
            }
        }
        public bool ResultEquals(Relation other)
        {
            return Statement.Equals(other.Statement);
        }
        public Relation? Deduce(Relation other)
        {
            if (Type == 'X' || other.Type == 'X')
            {
                return null;
            }
            List<char> implementedTypes = ['>', 'C', 'X', 'V', 'G', 'L', '='];
            if (!implementedTypes.Contains(Type) || !implementedTypes.Contains(other.Type))
            {
                throw new ArgumentException($"Unanticipated type pair ({Type}, {other.Type}).");
            }
            // Potential improvement:
            // Sentence? newStatement;
            // Then instead of 'return new(new(...), [Derivation, other.Derivation]);' we have 'newStatement = new(...); break;'
            // Then at the end we return new(newStatement, [Derivation,other.Derivation]. Cleaner looking code.
            switch (Type, other.Type)
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
                        return new(new('L', [Ids[0], other.Ids[0], Ids[2]]), [Derivation, other.Derivation]);
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
                    throw new ArgumentException($"this.Ids ({this.Ids}) \\cup other.Ids ({other.Ids}) == 3, but this.Ids \\cap other.Ids == \\emptyset...");
                default:
                    throw new NotImplementedException($"Relation {this} has unexpected type {Type}.");
                }
        }
    }
}

namespace CCView.CardinalData.Compute
{
    public class RelationDatabase
    {
        public Dictionary<int, CC> Cardinals { get; private set; } = [];
        public HashSet<AtomicRelation> AtomicRelations { get; private set; } = [];
        public HashSet<Relation> Relations { get; private set; } = [];
        public Dictionary<int, Article> Articles { get; private set; } = [];
        public Dictionary<int, Theorem> Theorems { get; set; } = [];
        public Dictionary<int, Model> Models { get; set; } = [];
        public Dictionary<(CC, CC), HashSet<CC>> Density { get; private set; } = [];
        private bool DynamicDensity { get; set; } = false;
        private bool TrivialRelationsCreated { get; set; } = false;

        public RelationDatabase(Dictionary<int, CC> cardinals, IEnumerable<Relation> relations, Dictionary<int, Article> articles, Dictionary<int, Theorem> theorems, Dictionary<int, Model> models)
        {
            Cardinals = cardinals;
            if (relations is HashSet<Relation> hash) Relations = hash;
            else Relations = relations.ToHashSet();
            Articles = articles;
            Theorems = theorems;
            Models = models;
        }
        public RelationDatabase()
        {
        }
        public CC CardinalFromSentence(Sentence sentence, int ind)
        {
            if (ind > 2 || ind == 0) throw new ArgumentException("ind must be 1 or 2.");
            return Cardinals[sentence.Ids[ind]];
        }
        // Cardinal from relation
        private CC CFR(Relation relation, int ind)
        {
            return CardinalFromSentence(relation.Statement, ind);
        }
        public bool PopulateDensity()
        {
            if (DynamicDensity) Program.LoadLog("In-betweenness relation already instantiated.");
            else
            {
                Program.LoadLog("Computing in-betweenness relation for cardinal characteristics.");
                Program.LoadLog("First computing transitive closure.");
                int n = LogicTransClose();
                foreach (Relation r1 in Relations)
                {
                    if (r1.Type == '>')
                    {
                        foreach (Relation r2 in Relations)
                        {
                            if (r2.Type == '>')
                            {
                                if (r1.Statement.GetItem2().Equals(r2.Statement.GetItem1()))
                                {
                                    if (Density.TryGetValue((CFR(r1, 1), CFR(r2, 2)), out HashSet<CC>? between))
                                    {
                                        between.Add(CFR(r1, 2));
                                    }
                                    else
                                    {
                                        Density[(CFR(r1, 1), CFR(r2, 2))] = [CFR(r1, 2)];
                                    }
                                }
                            }
                        }
                    }
                }
                Program.LoadLog("In-betweenness relation computed.");
                Program.LoadLog("The in-betweenness relation will continue to compute as the database is modified. This will also automatically maintain transitive closure.");
                DynamicDensity = true;
                return n > 0;
            }
            return false;
        }
        public int CreateTrivialRelations()
        {
            if (TrivialRelationsCreated) return 0;
            int numberOfRelations = Relations.Count;
            foreach (AtomicRelation a in AtomicRelations)
            {
                Relation newRel = new(a);
                if (!Relations.Any(r => r.Equals(newRel)))
                {
                    Relations.Add(newRel);
                }
            }
            TrivialRelationsCreated = true;
            return Relations.Count - numberOfRelations;
        }
        public static HashSet<Relation> ComputeDeductiveClosure(IEnumerable<Relation> relations)
        {
            HashSet<Relation> newRelations = [.. relations]; // This is short for new HashSet<Relation>(relation);
            Dictionary<Sentence, int> ageDict = [];
            foreach (Relation r in relations)
            {
                if (ageDict.TryGetValue(r.Statement, out int age))
                {
                    ageDict[r.Statement] = Math.Min(age, r.Age);
                }
                else
                {
                    ageDict[r.Statement] = r.Age;
                }
            }
            bool changed;
            do
            {
                changed = false;
                HashSet<Relation> newRelationsToAdd = [];
                foreach (var r1 in newRelations)
                {
                    foreach (var r2 in newRelations)
                    {
                        Relation? dedRel = r1.Deduce(r2);
                        if (dedRel is Relation newRel)
                        {
                            if (ageDict.TryGetValue(newRel.Statement, out int age))
                            {
                                if (newRel.Age < age)
                                {
                                    newRelationsToAdd.Add(newRel);
                                    ageDict[newRel.Statement] = newRel.Age;
                                    changed = true;
                                }
                            }
                            else
                            {
                                newRelationsToAdd.Add(newRel);
                                ageDict[newRel.Statement] = newRel.Age;
                                changed = true;
                            }
                        }
                    }
                }
                foreach (var r in newRelationsToAdd)
                {
                    newRelations.Add(r);
                }
            } while (changed);
            return newRelations;
        }
        public int LogicTransClose()
        {
            int n = Relations.Count;
            Relations = ComputeDeductiveClosure(Relations);
            int m = Relations.Count;
            Program.LoadLog($"Constructed {m - n} new relations in deductive closure.");
            return m - n;
        }
        public Relation AddCtoCRelation(CC? a, CC? b, char type, Theorem? witness)
        {
            if (!Sentence.CtoCTypes.Contains(type)) throw new ArgumentException($"AddCtoCRelation cannot be called with type {type}.");
            else if (a == null || b == null) throw new ArgumentException("Neither cardinal may be null.");
            else if (witness == null) throw new ArgumentException("The theorem may not be null.");
            else
            {
                Sentence statement = new(type, [a.Id, b.Id]);
                AtomicRelation atom = new(statement, witness);
                Relation newRelation = new(atom);
                Relations.Add(newRelation);
                return newRelation;
            }
        }
        public Relation AddMCNRelation(Model? m, CC? c, int n, char type, Theorem? witness)
        {
            if (!Sentence.MCNTypes.Contains(type)) throw new ArgumentException($"AddMCNRelation cannot be called with type {type}.");
            else if (m == null) throw new ArgumentException("The model cannot be null.");
            else if (c == null) throw new ArgumentException("The cardinal cannot be null.");
            else if (witness == null) throw new ArgumentException("The theorem cannot be null.");
            else
            {
                Sentence statement = new(type, [m.Id, c.Id, n]);
                AtomicRelation atom = new(statement, witness);
                Relation newRelation = new(atom);
                Relations.Add(newRelation);
                return newRelation;
            }
        }
        public static int NewDictId<T>(Dictionary<int, T> dict, bool fast = false)
        {
            if (fast)
            {
                return dict.Keys.Max() + 1;
            }
            else
            {
                var newId = 0;
                while (dict.ContainsKey(newId))
                {
                    newId++;
                }
                return newId;
            }
        }
        public Article AddArticle(Article article)
        {
            if (article.Id == -1)
            {
                int newId = NewDictId<Article>(Articles);
                article.Id = newId;
                Articles[newId] = article;
            }
            else
            {
                Articles[article.Id] = article;
            }
            return article;
        }
        public Article AddArticle(string? name, int date, string? citation, int id)
        {
            Article art = new(id, date, name ?? "No title provided", citation ?? "No citation provided");
            Articles[id] = art;
            return art;
        }
        public Article AddArticle(string? name, int date, string? citation)
        {
            Article art = new(NewDictId<Article>(Articles), date, name ?? "No title provided", citation ?? "No citation provided");
            Articles[art.Id] = art;
            return art;
        }
        public void AddCardinal(string? name, string? symbol, int id)
        {
            if (Cardinals.ContainsKey(id)) // Order of operations is important here or you'll get errors for id >= CCI.Count
            {
                throw new ArgumentException($"ID {id} is in use by {GetCardinalById(id)}.");
            }
            else
            {
                var newCardinal = new CC(id, name!, symbol!);
                Cardinals[id] = newCardinal; // It's important to do this before adding the cardinal
                Console.WriteLine($"Added new cardinal: {newCardinal}");
            }
        }
        public void AddCardinal(string? name, string? symbol, bool fast = false)
        {
            AddCardinal(name, symbol, NewDictId<CC>(Cardinals, fast));
        }
        public HashSet<Relation> GetMinimalRelations(Dictionary<int, CC> desiredCardinals)
        {
            return DynamicDensity // : ? is a ternary relation. P : A ? B means "IF P THEN A ELSE B".
                ? GraphAlgorithm.DensityTransitiveReduction(desiredCardinals, Relations, Density)
                : GraphAlgorithm.TransitiveReduction(desiredCardinals, Relations);
        }
        public static T? GetTByIdOrDefault<T>(Dictionary<int, T> dict, int id, T? def)
        {
            if (dict.TryGetValue(id, out T? match))
            {
                return match;
            }
            else
            {
                Console.WriteLine($"WARNING: No object type {typeof(T)} with id {id} found. Returning null.");
                return def;
            }
        }
        public CC? GetCardinalById(int id)
        {
            return GetTByIdOrDefault(Cardinals, id, null);
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
        public void AddCtoCRelationByIds(int id1, int id2, char type, int witnessId)
        {
            AddCtoCRelation(GetCardinalById(id1), GetCardinalById(id2), type, GetTheoremById(witnessId));
        }
        public void AddMCNRelationByIds(int modId, int ccId, int n, char type, int witnessId)
        {
            AddMCNRelation(GetModelById(modId), GetCardinalById(ccId), n, type, GetTheoremById(witnessId));
        }
        public CC GetCardinalBySymbol(string symbol)
        {
            CC? match = Cardinals.Values.FirstOrDefault(c => c.SymbolString == symbol);
            if (match != null)
            {
                return match;
            }
            else
            {
                throw new ArgumentException($"No cardinal with symbol {symbol} found.");
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
        public static HashSet<AtomicRelation> GenerateAtoms(Theorem theorem)
        {
            HashSet<AtomicRelation> newAtoms = [];
            foreach (var sentence in theorem.Results)
            {
                newAtoms.Add(new(sentence, theorem));
            }
            return newAtoms;
        }
    }
}
// Should this be moved to GraphLogic?
namespace CCView.CardinalData.QGInterface
{
    public class RelEdge : Edge<CC>
    {
        public Relation Relation { get; }
        public RelEdge(Relation relation, RelationDatabase rd) : base(rd.GetCardinalById(relation.Ids[0])!, rd.GetCardinalById(relation.Ids[1])!)
        {
            Relation = relation;
        }
        public override string ToString()
        {
            return Relation.ToString();
        }
    }
}