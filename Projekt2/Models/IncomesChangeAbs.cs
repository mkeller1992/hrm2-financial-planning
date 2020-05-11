using Microsoft.AspNetCore.Routing.Tree;
using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Projekt2.Constants;

namespace Projekt2.Models
{
    public class IncomesChangeAbs : IModificationOperation
    {
        private readonly int _amountToAdd;

        public IncomesChangeAbs(int amountToAdd)
        {
            _amountToAdd = amountToAdd;
        }

        public void ApplyModification(int financialYear, AccountYear accountPreviousYear, AccountYear accountSelectedYear)
        {
            var incomePreviousYear = accountPreviousYear.Year == financialYear ? accountPreviousYear.IncomeEffective : accountPreviousYear.IncomeBudget;
            accountSelectedYear.IncomeBudget = incomePreviousYear + _amountToAdd;
        }
    }
}
