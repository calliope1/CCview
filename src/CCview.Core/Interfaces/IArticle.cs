using Newtonsoft.Json.Linq;


namespace CCview.Core.Interfaces
{
    public interface IArticle
    {
        int GetBirthday();
        string GetName();
        string GetCitation();
        int GetId();
        void InstantiateFromJArray(JArray args);
    }
}
