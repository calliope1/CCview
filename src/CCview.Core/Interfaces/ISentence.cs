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

namespace CCview.Core.Interfaces
{
    // The "Sentence" is the most fundamental statement about cardinal characteristics
    // It is not saved, but it serves as the way of understanding how two cardinals are being related
    public interface ISentence
    {
        RelationType Relationship { get; }
        RelationType GetRelationType();
        IEnumerable<int> GetIds();
        static virtual IEnumerable<char> GetValidTypes()
        {
            throw new NotImplementedException("This method should be overridden in the derived class.");
        }
        static virtual bool IsValidType(char relationType)
        {
            throw new NotImplementedException("This method should be overridden in the derived class.");
        }
        static virtual bool IsCtoCType(char relationType)
        {
            throw new NotImplementedException("This method should be overridden in the derived class.");
        }
        static virtual bool IsMCNType(char relationType)
        {
              throw new NotImplementedException("This method should be overridden in the derived class.");
        }
        static virtual int TypeIdFromChar(char relationType)
        {
            throw new NotImplementedException("This method should be overridden in the derived class.");
        }
        int GetItem1();
        int GetItem2();
        int GetModel();
        int GetCardinal();
        int GetAleph();
    }
}