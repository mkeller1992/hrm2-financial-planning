using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using Projekt2.ViewModels;
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

        public ModificationUnit(IAccountParams accountParams, IModificationOperation modificationOperation)
        {
            _accountParams = accountParams;
            _modificationOperation = modificationOperation;
        }

        public void ExecuteChanges(int selectedYear, int financialYear, List<AccountYearDto> allAccounts)
        {
            List<AccountYearDto> accountsPreviousYear = GetRelevantAccounts(allAccounts, (selectedYear - 1));
            List<AccountYearDto> accountsSelectedYear = GetRelevantAccounts(allAccounts, selectedYear);

            if (accountsPreviousYear.Count == 0 || accountsSelectedYear.Count == 0)
            {
                return;
            }

            foreach (var baseAcc in accountsPreviousYear)
            {
                foreach (var acc in accountsSelectedYear)
                {
                    if (baseAcc.SubjectId == acc.SubjectId &&
                        baseAcc.IdOfParentFunctionGroup == acc.IdOfParentFunctionGroup)
                    {
                        _modificationOperation.ApplyModification(financialYear, baseAcc, acc);
                    }
                }
            }
        }

        private List<AccountYearDto> GetRelevantAccounts(List<AccountYearDto> allAccounts, int year)
        {
            List<AccountYearDto> relevantAccounts = new List<AccountYearDto>();

            foreach (var acc in allAccounts)
            {
                // if subjectId or functionGroupId == null, subjectId resp. functionGroupId is not relevant for the selection

                if ((_accountParams.SubjectId == null ||
                    _accountParams.SubjectId == acc.SubjectId.Substring(0, _accountParams.SubjectLevel)) &&
                   (_accountParams.IdOfParentFunctionGroup == null ||
                   _accountParams.IdOfParentFunctionGroup == acc.IdOfParentFunctionGroup.Substring(0, _accountParams.ParentFunctionGroupLevel)) &&
                    acc.Year == year)
                {
                    relevantAccounts.Add(acc);
                }
            }
            return relevantAccounts;
        }


    }
}
