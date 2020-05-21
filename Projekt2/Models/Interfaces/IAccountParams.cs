using Projekt2.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models.Interfaces
{
    public interface IAccountParams
    {
        public string SubjectId { get; set; }

        public int SubjectLevel { get; }

        public string IdOfParentFunctionGroup { get; set; }

        public int ParentFunctionGroupLevel { get; }

    }
}
