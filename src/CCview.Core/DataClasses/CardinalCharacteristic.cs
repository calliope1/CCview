using Newtonsoft.Json;
using CCview.Core.GraphLogic;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using CCview.Core.Services;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using CCview.Core.Interfaces;

namespace CCview.Core.DataClasses
{
    /// <summary>
    /// Cardinal characteristic
    /// </summary>
    public class CardinalCharacteristic : ICardinalCharacteristic
    {
        public int Id { get; private set; } = -1;
        public int GetId() => Id;
        public string Name { get; private set; } = "No name assigned";
        public string GetName() => Name;
        public string EquationSymbol { get; private set; } = "X";
        public string GetEquationSymbol() => EquationSymbol;
        public CardinalCharacteristic(JArray args)
        {
            Id = args[0].Value<int>();
            Name = args[1].Value<string>() ?? "No name assigned.";
            EquationSymbol = args[2].Value<string>() ?? "X";
        }
        //public void InstantiateFromJArray(JArray args)
        //{
        //    Id = args[0].Value<int>();
        //    Name = args[1].Value<string>() ?? "No name assigned.";
        //    EquationSymbol = args[2].Value<string>() ?? "X";
        //}
        public CardinalCharacteristic(int id, string name, string symbolString)
        {
            Id = id;
            Name = name;
            EquationSymbol = symbolString;
        }
        public override bool Equals(object? obj)
        {
            return obj is CC other && Id == other.GetId();
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name} ({EquationSymbol}, ID: {Id})";
        }
    }
}