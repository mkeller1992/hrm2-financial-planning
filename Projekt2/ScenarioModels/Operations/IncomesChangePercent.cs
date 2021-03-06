﻿using Projekt2.Constants;
using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class IncomesChangePercent : IModificationOperation
    {
        public string Description { get; }

        private decimal _changeInPercent;

        public IncomesChangePercent(decimal changeInPercent) 
        {
            _changeInPercent = changeInPercent;
            string suffix = changeInPercent > 0 ? Const.DescrPercentIncomeIncrease : Const.DescrPercentIncomeDecrease;
            Description = $"{changeInPercent}{suffix}";
        }

        public void ApplyModification(int financialYear, AccountYearDto accountPreviousYear, AccountYearDto accountSelectedYear)
        {
            decimal? baseValue = accountPreviousYear.Year == financialYear ?
                                 accountPreviousYear.IncomeActual : accountPreviousYear.IncomeBudget;

            if (baseValue == null || baseValue == 0) { return; }

            decimal increment = decimal.Multiply(decimal.Divide(baseValue.Value, 100), _changeInPercent);
            accountSelectedYear.IncomeBudget += increment;
        }
    }
}
