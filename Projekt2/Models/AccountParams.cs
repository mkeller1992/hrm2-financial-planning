using Projekt2.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class AccountParams : IAccountParams
    {
        public List<string> SubjectIds { get; set; }
        public string IdOfParentFunctionGroup { get; set; }

        private int _financialYear;
        public int FinancialYear
        {
            get
            {
                return _financialYear;
            }
            set
            {
                if (value != 0)
                {
                    _financialYear = value;
                    BudgetYears = new List<int> { (value + 1), (value + 2), (value + 3), (value + 5), (value + 6), };
                }
            }
        }

        public List<int> BudgetYears { get; private set; }
    }
}
