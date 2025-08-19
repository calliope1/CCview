using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.CLI.Errors
{
    internal static class ErrorNumbers
    {
        /// <summary>
        /// Used when a relation with the prescribed id does not exist.
        /// </summary>
        public static (int ErrorNumber, string ErrorMessage)
            RelationIdNotFound = (50, "Relation with the given id does not exist");
    }
}
