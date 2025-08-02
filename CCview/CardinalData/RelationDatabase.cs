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
        public int Id { get; private set; } = -1;
        public string Name { get; set; } = "No name assigned";
        public string SymbolString { get; set; } = "X";
        private int ArtId { get; set; } = -1;
        protected override List<string> FieldsToSave => ["Id", "Name", "SymbolString"];

        [JsonConstructor] // Telling Json.NET to use this constructor
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
        public int Id { get; private set; } = -1;
        // Generally represented as YYYYMMDD, with XX = 99 if not found. MaxValue for 'no year' for simplicity
        public int Date { get; private set; } = int.MaxValue;
        public string Name { get; private set; } = "Article name required!";
        public string Citation { get; private set; } = "Citation required!";
        private HashSet<Theorem> Results { get; set; } = []; // Lets keep this private until we know if we need it
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
            this.Id = rD.NewArtId(fast);
        }
    }

    // To-do list regarding the 'N' type relations:
    // // Transitive closure: Con(a > b) && b \geq c => Con(a > c)
    // // 'IsDerived' status in relations, with an associate List<Relation> showing their working out
    // // Maybe we have two dictionaries: CCStatus Dictionary<(CC, CC), Char> so that CCStatus[(a, b)] = 'X' iff there is a relation of type 'X',
    // // // and CCWitnesses Dictionary<(CC, CC), HashSet<Relation>> so that CCStatus[(a, b)] is all the relations between a and b.
    // // // We would need to figure out a strict hierarchy for relation types, but that should be possible with the relations in mind (i.e. =, >, N, =suc, etc)
    // // // This saves repeatedly asking about adjacency and possibly even saves memory.
    // // // We should also store the dictionaries I suppose. Should we give Relations ids?

    public class Relation : JsonCRTP<Relation>
    {
        public CC Item1 { get; set; }
        public int Item1Id { get; set; } = -1;
        public CC Item2 { get; set; }
        public int Item2Id { get; set; } = -1;
        public Char Type { get; set; }
        public int TypeId { get; set; } = -1;
        public HashSet<AtomicRelation> Derivation { get; set; } = [];
        public List<int[]> DerIds { get; set; } = [];
        public int Age => Derivation.Select(a => a.Witness.Article.Date).Max();
        public static List<Char> TypeIndices { get; private set; } = ['>','C'];
        // '>' refers to ZFC \vdash Item1 \geq Item2
        // 'C' refers to Con(ZFC + Item1 > Item2). That is, Item2 \geq Item1 is unprovable
        protected override List<string> FieldsToSave => ["Item1.Id", "Item2.Id", "TypeId", "DerIds"];
        //[JsonSaveableConstructor]
        public Relation(JArray args, List<CC> cardinals)
        {
            Item1Id = args[0].Value<int>();
            Item2Id = args[1].Value<int>();
            Item1 = cardinals[Item1Id];
            Item2 = cardinals[Item2Id];
            if (Item1.Id != Item1Id || Item2.Id != Item2Id)
            {
                // Before public release we'll want to instead throw a warning and then do a manual search
                throw new ArgumentException("Cardinals list mis-indexed. Cardinal with id i must be at index i.");
            }
            TypeId = args[2].Value<int>();
            Type = Relation.TypeIndices[TypeId];
            DerIds = args[3].Value<List<int[]>>() ?? [];
        }
        public Relation() { Item1 = new(); Item2 = new(); }
        public override void InstantiateFromJArray(JArray args)
        {
            //Console.WriteLine("WARNING: You are calling Relation.InstantiateFromJArray. This will not correctly instantiate the Item1 or Item2 variables.");
            Item1Id = args[0].Value<int>();
            Item2Id = args[1].Value<int>();
            //if (Item1.Id != Item1Id || Item2.Id != Item2Id)
            //{
            //    // Before public release we'll want to instead throw a warning and then do a manual search
            //    throw new ArgumentException("Cardinals list mis-indexed. Cardinal with id i must be at index i.");
            //}
            TypeId = args[2].Value<int>();
            Type = Relation.TypeIndices[TypeId];
        }
        public Relation(CC item1, CC item2, char type)
        {
            Item1 = item1;
            Item2 = item2;
            Type = type;
            TypeId = TypeIndices.IndexOf(type);
        }

        public override bool Equals(object? obj) // This needs to be overhauled because of 'signature's.
        {
            return obj is Relation other &&
                   Item1.Equals(other.Item1) &&
                   Item2.Equals(other.Item2) &&
                   Type == other.Type &&
                   Derivation == other.Derivation;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Item1, Item2, Type, Derivation); // Needs overhauling
        }
        public override string ToString()
        {
            return $"Relation type {Type} between {Item1} and {Item2}"; // Needs overhauling
        }
        // TypeIds *could* be shorts because Char and short are both 16 bit, but this is excessive
        public static int TypeIdFromChar(Char type)
        {
            return TypeIndices.First(x => x == type);
        }
    }
    // "Atomic" relation that is proved directly by a single model or theorem
    // AtomicRelations (and Relations) then derive further Relations that have a witnessing signature of atomic relations
    public class AtomicRelation
    {
        public CC Item1 { get; set; } = new();
        public CC Item2 { get; set; } = new();
        public Char Type { get; set; } = 'X';
        public Theorem Witness { get; set; } = new();
        public AtomicRelation(CC item1, CC item2, char type, Theorem witness)
        {
            Item1 = item1;
            Item2 = item2;
            Type = type;
            Witness = witness;
        }
        public AtomicRelation() { }
        public override bool Equals(object? obj)
        {
            return obj is AtomicRelation other
                && other.Item1.Equals(Item1)
                && other.Item2.Equals(Item2)
                && other.Type.Equals(Type)
                && other.Witness.Equals(Witness);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Item1, Item2, Type, Witness);
        }
        public override string ToString()
        {
            return $"Relation ID{Item1.Id} {Type} ID{Item2.Id} from ID{Witness.Id}";
        }
    }
    // "Atomic" unit of proof, one theorem or model that implies one or more relations directly
    public class Theorem : JsonCRTP<Theorem>
    {
        public int Id { get; set; } = -1;
        public int ArtId { get; set; } = -1;
        public Article Article { get; set; } = null!;
        // Results is directly proved relations between cardinals in the description
        // If there is a model that implicitly proves certain relations, use the Model subclass
        public HashSet<(CC, CC, Char)> Results { get; set; } = [];
        // A ResId is a (CC.Id, CC.Id, TypeId)
        public HashSet<int[]> ResIds { get; set; } = [];
        public string Description { get; set; } = "No description provided.";
        protected override List<string> FieldsToSave => ["Id", "ArtId", "ResIds", "Description"];
        public override void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            ArtId = args[1].Value<int>();
            ResIds = args[2].Value<HashSet<int[]>>() ?? [];
            Description = args[3].Value<string>() ?? "No description provided.";
        }
        public override bool Equals(object? obj)
        {
            return obj is Theorem other && Id.Equals(other.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"Theorem ID:{Id} '{Description}' of {Article}";
        }
        public Theorem(int id, Article article, HashSet<(CC, CC, char)> results, string description)
        {
            Id = id;
            Article = article;
            Results = results;
            Description = description;
            ArtId = Article.Id;
            ResIds = Results.Select<(CC, CC, char), int[]>(r => [r.Item1.Id, r.Item2.Id, Relation.TypeIdFromChar(r.Item3)]).ToHashSet();
        }
        public Theorem() { }
    }
    public class Model : Theorem
    {
        // The idea with CardinalValues is that each set in the list is an equivalence class by equipotence
        // Then if one set comes before another, the cardinals in the first set are less than those in the second
        public List<HashSet<CC>> Values { get; set; } = [];
        public List<HashSet<int>> ValIds { get; set; } = [];
        protected override List<string> FieldsToSave => ["Id", "Article.Id", "ResIds", "Description", "ValIds"];
        private readonly int Cid = Relation.TypeIdFromChar('C');
        public Model(int id, Article article, HashSet<(CC, CC, char)> results, string description, List<HashSet<CC>> values)
            : base(id, article, results, description)
        {
            Values = values;
            ArtId = article.Id;
            ResIds = Results.Select<(CC, CC, char), int[]>(r => [r.Item1.Id, r.Item2.Id, Relation.TypeIdFromChar(r.Item3)]).ToHashSet();
            ValIds = Values.Select(v => v.Select(w => w.Id).ToHashSet()).ToList();
        }
        public override void InstantiateFromJArray(JArray args)
        {
            Id = args[0].Value<int>();
            ArtId = args[1].Value<int>();
            ResIds = args[2].Value<HashSet<int[]>>() ?? [];
            Description = args[3].Value<string>() ?? "No description provided!";
            ValIds = args[4].Value<List<HashSet<int>>>() ?? [];
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
        public void GenerateResults()
        {
            for (int i = 0; i < Values.Count; i++)
            {
                for (int j = i + 1; j < Values.Count; j++)
                {
                    foreach (CC smaller in Values[i])
                    {
                        foreach (CC larger in Values[j])
                        {
                            Results.Add(new(larger, smaller, 'C'));
                            ResIds.Add([larger.Id, smaller.Id, Cid]);
                        }
                    }
                }
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

        public RelationDatabase(IEnumerable<CC> cardinals, IEnumerable<Relation> relations, IEnumerable<Article> articles, IEnumerable<Theorem> theorems, IEnumerable<Model> models)
        {
            foreach (CC c in cardinals)
            {
                Cardinals[c.Id] = c;
            }

            foreach (var relation in relations)
            {
                Relations.Add(relation);
                if (!Cardinals.ContainsKey(relation.Item1.Id))
                    Cardinals[relation.Item1.Id] = relation.Item1;
                if (!Cardinals.ContainsKey(relation.Item2.Id))
                    Cardinals[relation.Item2.Id] = relation.Item2;
            }
            foreach (Article a in articles)
            {
                Articles[a.Id] = a;
            }
            foreach (Theorem t in theorems)
            {
                Theorems[t.Id] = t;
            }
            foreach (Model m in models)
            {
                Models[m.Id] = m;
            }
        }
        public RelationDatabase()
        {
        }
        
        public static List<int> InitIndexList<T>(List<T> values, Func<T, int> idFinder, int defaultValue = -1)
        {
            if (values.Count == 0)
            {
                return [];
            }
            List<int> ids = [.. values.Select(v => idFinder(v))];
            int maxId = ids.Max();
            List<int> IndexingList = [.. Enumerable.Repeat(defaultValue, maxId + 1)];
            for (int i = 0; i < ids.Count; i++)
            {
                IndexingList[ids[i]] = i;
            }
            return IndexingList;
        }
        public bool PopulateDensity()
        {
            if (DynamicDensity) Program.LoadLog("In-betweenness relation already instantiated.");
            else
            {
                Program.LoadLog("Computing in-betweenness relation for cardinal characteristics.");
                Program.LoadLog("First computing transitive closure.");
                int n = TransClose();
                foreach (Relation r1 in Relations)
                {
                    foreach (Relation r2 in Relations)
                    {
                        if (r1.Item2.Equals(r2.Item1) && r1.Type == '>' && r2.Type == '>')
                        {
                            if (Density.TryGetValue((r1.Item1, r2.Item2), out HashSet<CC>? between))
                            {
                                between.Add(r1.Item2);
                            }
                            else
                            {
                                Density[(r1.Item1, r2.Item2)] = [r1.Item2];
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
        public static HashSet<Relation> ComputeTransitiveClosure(IEnumerable<Relation> relation)
        {
            HashSet<Relation> newRelations = [.. relation]; // This is short for new HashSet<Relation>(relation);
            Relation testRelation = new(new CC(), new CC(), '>'); // This is to save memory
            bool changed;
            do
            {
                changed = false;
                HashSet<Relation> newRelationsToAdd = [];
                foreach (var relOne in newRelations)
                {
                    foreach (var relTwo in newRelations)
                    {
                        // a C b > c implies a C c
                        // a > b > c implies a > c
                        // a C b > a is impossible
                        if (relOne.Type == 'C' && relTwo.Type == '>')
                        {
                            if (relOne.Item2.Equals(relTwo.Item1)) throw new DataMisalignedException($"Relations {relOne} and {relTwo} are incompatible.");
                        }
                        else if (relOne.Type == 'C' && relTwo.Type == '>')
                        {
                            if (relOne.Item2.Equals(relTwo.Item1))
                            {
                                testRelation.Item1 = relOne.Item1;
                                testRelation.Item2 = relTwo.Item2;
                                testRelation.Type = 'C';
                                if (!newRelations.Contains(testRelation))
                                {
                                    newRelationsToAdd.Add(new Relation(relOne.Item1, relTwo.Item2, 'C'));
                                    changed = true;
                                }
                            }
                        }
                        else if (relOne.Type == '>' && relTwo.Type == '>')
                        {
                            testRelation.Item1 = relOne.Item1;
                            testRelation.Item2 = relTwo.Item2;
                            testRelation.Type = '>';
                            if (relOne.Item2.Equals(relTwo.Item1)
                                && !newRelations.Contains(testRelation)
                                && !relOne.Item1.Equals(relTwo.Item2))
                            {
                                newRelationsToAdd.Add(new Relation(relOne.Item1, relTwo.Item2, relOne.Type));
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
        public int TransClose()
        {
            Console.WriteLine("HEY! Use TransitiveClosureAlgorithm from QuikGraph (if that works).");
            int n = Relations.Count();
            Relations = ComputeTransitiveClosure(Relations);
            int m = Relations.Count();
            Program.LoadLog($"Constructed {m - n} new relations in transitive closure.");
            return m - n;
        }

        public void AddRelation(CC? a, CC? b, char type, bool lazy = true)
        {
            if (a == null || b == null)
            {
                throw new ArgumentException("Neither cardinal may be null.");
            }
            else if (!Cardinals.ContainsKey(a.Id) || !Cardinals.ContainsKey(b.Id))
            {
                throw new ArgumentException("Both cardinals must be part of the relations.");
            }
            HashSet<Relation> toAdd = [new(a, b, type)];
            if (type == '>' && DynamicDensity)
            {
                List<CC> intoA = [];
                List<CC> outOfB = [];
                // In this case we may assume that Relations is already transitive
                foreach (Relation r in Relations)
                {
                    if (r.Type == '>')
                    {
                        if (r.Item2.Equals(a))
                        {
                            intoA.Add(r.Item1);
                            toAdd.Add(new(r.Item1, b, '>'));
                        }
                        else if (r.Item1.Equals(b))
                        {
                            outOfB.Add(r.Item2);
                            toAdd.Add(new(a, r.Item2, '>'));
                        }
                    }
                }
                foreach (CC cIn in intoA)
                {
                    foreach (CC cOut in outOfB)
                    {
                        toAdd.Add(new(cIn, cOut, '>'));
                    }
                }
                foreach (var rel in toAdd)
                {
                    Relations.Add(rel);
                }
            }
            else if (!lazy)
            {
                foreach (var rel in toAdd)
                {
                    Relations.Add(rel);
                }
                Relations = ComputeTransitiveClosure(Relations);
            }
            else
            {
                foreach (var rel in toAdd)
                {
                    Relations.Add(rel);
                }
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
        public int NewCCId(bool fast = false)
        {
            return NewDictId<CC>(Cardinals, fast);
        }
        public int NewArtId(bool fast = false)
        {
            return NewDictId<Article>(Articles, fast);
        }
        public Article AddArticleIdSafe(Article article)
        {
            int id = article.Id;
            if (id == -1 || Articles.ContainsKey(id))
            {
                article.GetNewId(this);
            }
            Articles[article.Id] = article;
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
            Article art = new(NewArtId(), date, name ?? "No title provided", citation ?? "No citation provided");
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
            AddCardinal(name, symbol, NewCCId(fast));
        }
        public bool IsRelated(CC a, CC b, char type)
        {
            return Relations.Contains(new Relation(a, b, type));
        }
        public HashSet<Relation> GetMinimalRelations(List<CC> desiredCardinals)
        {
            return DynamicDensity // : ? is a ternary relation. P : A ? B means "IF P THEN A ELSE B".
                ? GraphAlgorithm.DensityTransitiveReduction(desiredCardinals, Relations, Density)
                : GraphAlgorithm.TransitiveReduction(desiredCardinals, Relations);
        }
        public CC? GetCardinalById(int id)
        {
            if (Cardinals.TryGetValue(id, out CC? match))
            {
                return match;
            }
            else
            {
                Console.WriteLine($"WARNING: No cardinal with id {id} found. Returning null.");
                return null;
            }
        }
        public Article? GetArticleById(int id)
        {
            if (Articles.TryGetValue(id, out Article? match))
            {
                return match;
            }
            else
            {
                Console.WriteLine($"WARNING: No article with id {id} found. Returning null.");
                return null;
            }
        }

        public void AddRelationByIds(int id1, int id2, char type)
        {
            AddRelation(GetCardinalById(id1), GetCardinalById(id2), type);
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
        public Article? MisalignedArtIndsandArt(int id)
        {
            throw new InvalidOperationException("List ArtsInd and Articles are mis-aligned. This is a developer error, if you see this in typical use please submit a bug report.");
        }
        public void GenerateAtoms()
        {
            foreach (Theorem thm in Theorems.Values.Union(Models.Values))
            {
                AtomicRelations.UnionWith(GenerateAtoms(thm));
            }
        }
        public static HashSet<AtomicRelation> GenerateAtoms(Theorem theorem)
        {
            HashSet<AtomicRelation> newAtoms = [];
            foreach (var tup in theorem.Results)
            {
                newAtoms.Add(new(tup.Item1, tup.Item2, tup.Item3, theorem));
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
        public RelEdge(Relation relation) : base(relation.Item1, relation.Item2)
        {
            Relation = relation;
        }
        public override string ToString()
        {
            return Relation.ToString();
        }
    }
}