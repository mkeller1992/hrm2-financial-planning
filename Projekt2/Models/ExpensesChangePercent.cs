using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class ExpensesChangePercent : IModificationOperation
    {
        public string Description { get; }

        private decimal _changeInPercent;

        public ExpensesChangePercent(decimal changeInPercent)
        {
            _changeInPercent = changeInPercent;
            string suffix = changeInPercent > 0 ? "Aufwandsteigerung" : "Aufwandrückgang";
            Description = $"{changeInPercent}% {suffix}/Jahr";
        }

        public void ApplyModification(int financialYear, AccountYearDto accountPreviousYear, AccountYearDto accountSelectedYear)
        {
            decimal? baseValue = accountPreviousYear.Year == financialYear ?
                                 accountPreviousYear.ExpensesActual : accountPreviousYear.ExpensesBudget;

            if (baseValue == null || baseValue == 0) { return; }

            decimal increment = decimal.Multiply(decimal.Divide(baseValue.Value, 100), _changeInPercent);
            accountSelectedYear.ExpensesBudget += increment;
        }
    }
}
