using Projekt2.Constants;
using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class ExpensesChangeAbs : IModificationOperation
    {
        private readonly int _amountToAdd;

        public ExpensesChangeAbs(int amountToAdd)
        {
            _amountToAdd = amountToAdd;
        }

        public void ApplyModification(int financialYear, AccountYear accountPreviousYear, AccountYear accountSelectedYear)
        {
            var expensesPreviousYear = accountPreviousYear.Year == financialYear ? accountPreviousYear.ExpensesEffective : accountPreviousYear.ExpensesBudget;
            accountSelectedYear.ExpensesBudget = expensesPreviousYear + _amountToAdd;
        }
    }
}
