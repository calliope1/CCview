using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.Core.Interfaces
{
    public interface IRelationType
    {
        char GetSymbol();
        int GetIndex();
        string GetFamily();
        abstract static bool IsCtoC(char c);
        abstract static bool IsCtoCFromIndex(int i);
        abstract static bool IsMCN(char c);
        abstract static bool IsMCNFromIndex(int i);
        abstract static HashSet<char> AnticipatedTypes { get; }
        abstract static int IndexFromChar(char c);
        abstract static HashSet<char> CtoCTypes { get; }
        abstract static HashSet<char> MCNTypes { get; }
    }
}
