using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class ModificationUnit
    {
        protected readonly IAccountParams _accountParams;
        protected readonly IModificationOperation _modificationOperation;

        protected readonly List<AccountYear> _relevantAccounts; // Includes Accounts of Base-Year + later years


        public ModificationUnit(IAccountParams accountParams, IModificationOperation modificationOperation)
        {
            _accountParams = accountParams;
            _modificationOperation = modificationOperation;
            _relevantAccounts = new List<AccountYear>();
        }

        public void AddRelevantAccounts(List<AccountYear> allAccounts)
        {
            foreach (var acc in allAccounts)
            {
                if (_accountParams.SubjectIds.Contains(acc.SubjectId) &&
                   acc.FunctionId == _accountParams.IdOfParentFunctionGroup &&
                   (acc.Year == _accountParams.FinancialYear || _accountParams.BudgetYears.Contains(acc.Year)))
                {
                    _relevantAccounts.Add(acc);
                }
            }
        }

        public void ExecuteChanges(int year)
        {
            if (_relevantAccounts == null || _relevantAccounts.Count == 0)
            {
                throw new Exception("Relevant Accounts not set!");
            }

            List<AccountYear> accountsPreviousYear = _relevantAccounts.Where(a => a.Year == (year - 1)).ToList();

            foreach (var baseAcc in accountsPreviousYear)
            {
                foreach (var acc in _relevantAccounts)
                {
                    if (baseAcc.SubjectId == acc.SubjectId &&
                        baseAcc.FunctionId == acc.FunctionId &&
                        baseAcc.Year == year)
                    {
                        _modificationOperation.ApplyModification(_accountParams.FinancialYear, baseAcc, acc);
                    }
                }
            }
        }


    }
}
