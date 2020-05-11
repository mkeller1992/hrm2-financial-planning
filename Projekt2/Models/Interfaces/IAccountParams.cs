using Projekt2.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models.Interfaces
{
    public interface IAccountParams
    {
        public List<string> SubjectIds { get; set; }

        public string IdOfParentFunctionGroup { get; set; }

        public int FinancialYear { get; set; }

        public List<int> BudgetYears { get; }

    }
}
