using CCview.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.Core.DataClasses
{
    public class RelationType : IRelationType
    {
        /// <summary>
        /// Type, Ids:
        /// >, [x.Id, y.Id] means 'CC x, CC y, and ZFC proves x \geq y'
        /// C, [x.Id, y.Id] means 'CC x, CC y, and Con(ZFC + x > y)'
        /// X, [any] means 'You forgot to assign a Type'
        /// V, [m.Id, x.Id, n] means 'CC x, Model m, and m \models x = \aleph_n'
        /// G, [m.Id, x.Id, n] means 'CC x, Model m, and m \models x \geq \aleph_n'
        /// L, [m.Id, x.Id, n] means 'CC x, Model m, and m \models x \leq \aleph_n'
        /// =, [x.Id, y.Id] means 'CC x, CC y, and ZFC proves x = y'
        ///
        /// "CtoC" means "Cardinal-to-Cardinal". These are relations between two cardinal
        /// characteristics in ZFC.
        ///
        /// "MCN" means "Model-cardinal-number". These are relations between a cardinal
        /// and an aleph number in a given model of ZFC.
        /// 
        /// </summary>
        private static readonly Dictionary<char, (string Family, int Index)> RelationTypes = new()
        {
            { '>', ("CtoC", 0) },
            { 'C', ("CtoC", 1) },
            { 'X', ("None", 2) },
            { 'V', ("MCN", 3) },
            { 'G', ("MCN", 4) },
            { 'L', ("MCN", 5) },
            { '=', ("CtoC", 6) }
        };
        private static readonly Dictionary<int, (string Family, char Symbol)> ReverseRelationTypes =
            RelationTypes.Select(static kvp => (kvp.Value.Index, (kvp.Value.Family, kvp.Key))).ToDictionary();
        public static HashSet<char> CtoCTypes { get; } = [.. RelationTypes
            .Where(kvp => kvp.Value.Family.Equals("CtoC"))
            .Select(kvp => kvp.Key)];
        public static HashSet<char> MCNTypes { get; } = [.. RelationTypes
            .Where(kvp => kvp.Value.Family.Equals("MCN"))
            .Select(kvp => kvp.Key)];
        public static HashSet<char> AnticipatedTypes { get; } = [.. RelationTypes.Keys];
        public static bool IsCtoC(char symbol)
        {
            if (RelationTypes.TryGetValue(symbol, out var result))
            {
                return result.Family.Equals("CtoC");
            }
            Console.WriteLine($"WARNING: {symbol} is not a valid relationship symbol.");
            return false;
        }
        public static bool IsCtoCFromIndex(int index)
        {
            if (ReverseRelationTypes.TryGetValue(index, out var result))
            {
                return result.Family.Equals("CtoC");
            }
            Console.WriteLine($"WARNING: {index} is not a valid relationship index.");
            return false;
        }
        public static bool IsMCN(char symbol)
        {
            if (RelationTypes.TryGetValue(symbol, out var result))
            {
                return result.Family.Equals("MCN");
            }
            Console.WriteLine($"WARNING: {symbol} is not a valid relationship symbol.");
            return false;
        }
        public static bool IsMCNFromIndex(int index)
        {
            if (ReverseRelationTypes.TryGetValue(index, out var result))
            {
                return result.Family.Equals("MCN");
            }
            Console.WriteLine($"WARNING: {index} is not a valid relationship index.");
            return false;
        }
        public static int IndexFromChar(char symbol)
        {
            if (RelationTypes.TryGetValue(symbol, out var value))
            {
                return value.Index;
            }
            throw new ArgumentException($"{symbol} is not a valid relationship symbol.");
        }
        public char Symbol { get; }
        public RelationType(char relationChar)
        {
            if (!RelationTypes.ContainsKey(relationChar))
            {
                throw new ArgumentException($"Relation type {relationChar} does not exist.");
            }
            Symbol = relationChar;
        }
        public RelationType(int relationIndex)
        {
            if (!ReverseRelationTypes.TryGetValue(relationIndex, out var value))
            {
                throw new ArgumentException($"Relation type with index {relationIndex} does not exist.");
            }
            Symbol = value.Symbol;
        }
        public char GetSymbol()
        {
            return Symbol;
        }
        public int GetIndex()
        {
            return RelationTypes[Symbol].Index;
        }
        public string GetFamily()
        {
            return RelationTypes[Symbol].Family;
        }
        public override string ToString()
        {
            return Symbol.ToString();
        }
        public override bool Equals(object? obj)
        {
            return obj is RelationType other
                && other.Symbol == Symbol;
        }
        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }
    }
}
