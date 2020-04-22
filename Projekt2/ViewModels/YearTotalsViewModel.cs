using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.ViewModels
{
    public class YearTotalsViewModel
    {
        public int Year { get; set; }
        public decimal? ExpensesActualTotal { get; set; }
        public decimal? ExpensesBudgetTotal { get; set; }
        public decimal? IncomeActualTotal { get; set; }
        public decimal? IncomeBudgetTotal { get; set; }

        public decimal? BalanceActualTotal { get; set; }
        public decimal? BalanceBudgetTotal { get; set; }
    }
}
