using CCview.CLI.Commands;
using CCview.Core.DataClasses;
using CCview.Core.Services;
using System;
using System.CommandLine;
using ICC = CCview.Core.Interfaces.ICardinalCharacteristic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using CCview.Core.Interfaces;
using CCview.CLI.Errors;

namespace CCview.CLI.Commands
{
    public class ListCommand : Command
    {
        public ListCommand(Func<IRelationDatabase> getRelationDatabase) : base("List", "List objects of a particular type in the database.")
        {
            Subcommands.Add(new ListCardinalsCommand(getRelationDatabase));
        }
    }
    public class ListCardinalsCommand : Command
    {
        public ListCardinalsCommand(Func<IRelationDatabase> getRelationDatabase)
            : base("cardinals", "Lists all cardinal characteristics.")
        {
            SetAction(parseResult =>
            {
                IRelationDatabase relationDatabase = getRelationDatabase();
                foreach (CC cardinal in relationDatabase.GetCardinals().Values)
                {
                    Console.WriteLine($"{cardinal.Name} ({cardinal.EquationSymbol}, {cardinal.Id})");
                }
                return 0;
            });
        }
    }
    public class ListRelationsCommand : Command
    {
        public ListRelationsCommand(Func<IRelationDatabase> getRelationDatabase)
            : base("relations", "List all relations")
        {
            Option<bool> noSymbolsOption = new("--noSymbols", "-ns");
            SetAction(parseResult =>
            {
                IRelationDatabase relationDatabase = getRelationDatabase();
                IEnumerable<Relation> relations = relationDatabase.GetRelations().Values;
                if (!parseResult.GetValue(noSymbolsOption))
                {
                    IReadOnlyDictionary<int, CC> cardinals = relationDatabase.GetCardinals();
                    foreach (Relation r in relations)
                    {
                        Console.WriteLine(r.ToStringWithSymbols(cardinals));
                    }
                }
                else
                {
                    foreach (Relation r in relations)
                    {
                        Console.WriteLine(r);
                    }
                }
                return 0;
            });
        }
    }
    public class ListRelation : Command
    {
        public ListRelation(Func<IRelationDatabase> getRelationDatabase)
            : base("relation", "Get more information about a given relation.")
        {
            Argument<int> idArgument = new("id");
            SetAction(parseResult =>
            {
                int id = parseResult.GetValue(idArgument);
                IRelationDatabase relationDatabase = getRelationDatabase();
                Relation? relation = relationDatabase.GetRelationById(id);
                if (relation != null)
                {
                    Console.WriteLine(relation.ToVerboseString(relationDatabase));
                    return 0;
                }
                Console.WriteLine($"{id} is not the id of a relation.");
                // 50: 5 = Relation class; 0 = Id.
                return ErrorNumbers.RelationIdNotFound.ErrorNumber;
            });
        }
    }
    public class ListArticles : Command
    {
        public ListArticles(Func<IRelationDatabase> getRelationDatabase)
            : base("articles", "List of articles in the database.")
        {
            SetAction(parseResult =>
            {
                IRelationDatabase relationDatabase = getRelationDatabase();
                foreach (Article a in relationDatabase.GetArticles().Values)
                {
                    if (a.Id < 0) { continue; }
                    Console.WriteLine(a);
                }
                Console.WriteLine("Other 'articles' may exist with ids less than 0. These are internal logical articles, such as ID-2 ('folklore') and ID-3 ('unknown').");
                return 0;
            });
        }
    }
    public class ListTheorems : Command
    {
        public ListTheorems(Func<IRelationDatabase> getRelationDatabase)
            : base("theorems", "List of theorems in the database.")
        {
            SetAction(parseResult =>
            {
                IRelationDatabase relationDatabase = getRelationDatabase();
                foreach (Theorem t in relationDatabase.GetTheorems().Values)
                {
                    if (t.Id < 0) { continue; }
                    Console.WriteLine(t);
                }
                Console.WriteLine("Other 'theorems' may exist with ids less than 0. These are internal logical theorems, such as ID-2 ('folklore') and ID-3 ('unknown').");
                return 0;
            });
        }
    }
    //public class ListSomething : Command
    //{
    //    public ListSomething(Func<IRelationDatabase> getRelationDatabase)
    //        : base("listTemplate", "Just copy/paste me!")
    //    {
    //        SetAction(parseResult =>
    //        {
    //            IRelationDatabase relationDatabase = getRelationDatabase();
    //            return 0;
    //        });
    //    }
    //}
}

//Command listModels = new("models", "List of models in the database.");
//listCommand.Subcommands.Add(listModels);

//listModels.SetAction(parseResult =>
//{
//    foreach (Model m in env.Models.Values)
//    {
//        Console.WriteLine(m);
//    }
//    return 0;
//});

//Command listBetween = new("between", "List all cardinals lying between two given ids.")
//            {
//                idArgument,
//                idArgumentTwo
//            };
//listCommand.Subcommands.Add(listBetween);
//listBetween.Aliases.Add("btwn");

//listBetween.SetAction(parseResult =>
//{
//    env.ListBetween(parseResult.GetValue(idArgument), parseResult.GetValue(idArgumentTwo));
//});

//Command listTypes = new("types", "List all valid relation symbols.");
//listCommand.Subcommands.Add(listTypes);

//listTypes.SetAction(parseResult =>
//{
//    List<char> parseResultintedTypes = [];

//    string CtoCWriteString = "Cardinal-to-cardinal relations: ";
//    foreach (char type in Sentence.CtoCTypes)
//    {
//        CtoCWriteString += type + ", ";
//        parseResultintedTypes.Add(type);
//    }
//    Console.WriteLine(CtoCWriteString[..(Math.Min(0, CtoCWriteString.Length - 2))]);

//    string MCNWriteString = "Model-cardinal-aleph relations: ";
//    foreach (char type in Sentence.MCNTypes)
//    {
//        MCNWriteString += type + ", ";
//        parseResultintedTypes.Add(type);
//    }
//    Console.WriteLine(MCNWriteString[..(Math.Min(0, MCNWriteString.Length - 2))]);

//    if (Sentence.TypeIndices.Count == parseResultintedTypes.Count + 1) return;

//    string UnexpectedTypes = "Unanticipated types (consider submitting a bug report): ";
//    foreach (char type in Sentence.TypeIndices.Where(t => !t.Equals('X') && !parseResultintedTypes.Contains(t)))
//    {
//        UnexpectedTypes += type + ", ";
//    }
//    Console.WriteLine(UnexpectedTypes[..(Math.Min(0, UnexpectedTypes.Length - 2))]);
//});