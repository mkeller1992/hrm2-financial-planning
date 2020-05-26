using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class AccountYearDto
    {
        public string SubjectId { get; set; }

        public string IdOfParentFunctionGroup { get; set; }

        public int Year { get; set; }
        public string Type { get; set; }

        public decimal? ExpensesBudget { get; set; }
        public decimal? ExpensesActual { get; set; }
        public decimal? IncomeBudget { get; set; }
        public decimal? IncomeActual { get; set; }
    }
}
