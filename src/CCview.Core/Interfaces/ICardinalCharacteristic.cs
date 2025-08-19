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

namespace CCview.Core.Interfaces
{
    /// <summary>
    /// Cardinal characteristic
    /// </summary>
    public interface ICardinalCharacteristic
    {
        //public int Id { get; private set; } = -1;
        public string GetName();
        public string GetEquationSymbol();
        public int GetId();
        //public void InstantiateFromJArray(JArray args);
    }
}