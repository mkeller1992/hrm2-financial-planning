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
        public string Title { get; }

        public IAccountParams AccountParams { get;  }
        public IModificationOperation ModificationOperation { get; }

        public ModificationUnit(string title, IAccountParams accountParams, IModificationOperation modificationOperation)
        {
            Title = title;
            AccountParams = accountParams;
            ModificationOperation = modificationOperation;
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
                        ModificationOperation.ApplyModification(financialYear, baseAcc, acc);
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

                if ((AccountParams.SubjectId == null ||
                    AccountParams.SubjectId == acc.SubjectId.Substring(0, AccountParams.SubjectLevel)) &&
                   (AccountParams.IdOfParentFunctionGroup == null ||
                   AccountParams.IdOfParentFunctionGroup == acc.IdOfParentFunctionGroup.Substring(0, AccountParams.ParentFunctionGroupLevel)) &&
                    acc.Year == year)
                {
                    relevantAccounts.Add(acc);
                }
            }
            return relevantAccounts;
        }


    }
}
