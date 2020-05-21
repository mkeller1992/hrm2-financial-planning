using Projekt2.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class AccountParams : IAccountParams
    {
        /* Changes can be defined for subjects of different level and for parent-functionGroups of different levels */

        public string SubjectId { get; set; }

        public int SubjectLevel => SubjectId.Length;

        public string IdOfParentFunctionGroup { get; set; }

        public int ParentFunctionGroupLevel => IdOfParentFunctionGroup.Length;

    }
}
