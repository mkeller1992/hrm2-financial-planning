using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.ViewModels
{
    public class YearTotalsViewModel
    {
        public int Year { get; set; }
        public bool HasPreviousYear { get; set; }

        public decimal? ExpensesActualTotal { get; set; }
        public decimal? PercentageChangeExpensesActualTotal { get; set; }

        public decimal? ExpensesBudgetTotal { get; set; }
        public decimal? PercentageChangeExpensesBudgetTotal { get; set; }

        public decimal? IncomeActualTotal { get; set; }
        public decimal? PercentageChangeIncomeActualTotal { get; set; }

        public decimal? IncomeBudgetTotal { get; set; }
        public decimal? PercentageChangeIncomeBudgetTotal { get; set; }

        public decimal? BalanceActualTotal { get; set; }
        public decimal? PercentageChangeBalanceActualTotal { get; set; }


        public decimal? BalanceBudgetTotal { get; set; }
        public decimal? PercentageChangeBalanceBudgetTotal { get; set; }
    }
}
