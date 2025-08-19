using Newtonsoft.Json;
using CC = CCview.Core.DataClasses.CardinalCharacteristic;
using QuikGraph;
using Newtonsoft.Json.Linq;
using CCview.Core.JsonHandler;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using CCview.Core.DataClasses;
using ICC = CCview.Core.Interfaces.ICardinalCharacteristic;

namespace CCview.Core.Interfaces
{
    /// <summary>
    /// Model of ZFC, such as Cohen's model. Has parent <see cref="CCView.CardinalData.Article"/> in which
    /// it was first described.
    /// </summary>
    public interface IModel
    {
        int GetId();
        int GetArticleId();
        string GetDescription();
        HashSet<ModelValue> GetValIds();
        void InstantiateFromJArray(JArray args);
    }
}