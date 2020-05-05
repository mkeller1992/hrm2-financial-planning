using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.ViewModels
{
    public class AccountYearViewModel
    {
        public string Type { get; set; }
        public string AccountId { get; set; }
        public string AccountName { get; set; }
        public int AccountLevel { get; set; }
        public string ParentId { get; set; }
        public int Year { get; set; }
        public decimal? ExpensesBudget { get; set; }
        public decimal? PercentageChangeExpensesBudget { get; set; }

        public decimal? IncomeBudget { get; set; }
        public decimal? PercentageChangeIncomeBudget { get; set; }


        public decimal? ExpensesActual { get; set; }
        public decimal? PercentageChangeExpensesActual { get; set; }


        public decimal? IncomeActual { get; set; }
        public decimal? PercentageChangeIncomeActual { get; set; }


        public decimal? BalanceActual { get; set; }
        public decimal? PercentageChangeBalanceActual { get; set; }

        public decimal? BalanceBudget { get; set; }
        public decimal? PercentageChangeBalanceBudget { get; set; }


        public List<AccountYearViewModel> ChildAccounts { get; set; }

    }
}
