using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class Scenario
    {
        public int FinancialYear { get; }
        public List<int> BudgetYears { get; }
        private List<ModificationUnit> _modificationUnits { get; set; }

        public Scenario(int financialYear)
        {
            FinancialYear = financialYear;
            if (financialYear > 2000)
            {
                BudgetYears = Enumerable.Range(financialYear + 1, Constants.Const.NumberOfRelevantBudgetYears).ToList();
            }
            _modificationUnits = new List<ModificationUnit>();
        }


        public void AddModificationUnit(ModificationUnit m)
        {
            _modificationUnits.Add(m);
        }

        public List<AccountYearDto> AddScenarioAccounts(List<AccountYearDto> accountsForScenario)
        {
            if (_modificationUnits == null)
            {
                throw new Exception("No changes defined!");
            }

            // Add and calculate results year by year:

            if (BudgetYears != null)
            {
                foreach (var y in BudgetYears)
                {
                    // Add accounts for budget-year y:
                    AddAccountsForYear(accountsForScenario, y);

                    // Compute accounts for budget-year y:
                    foreach (var modifUnit in _modificationUnits)
                    {
                        modifUnit.ExecuteChanges(y, FinancialYear, accountsForScenario);
                    }
                }
            }

            return accountsForScenario;
        }


        private void AddAccountsForYear(List<AccountYearDto> accountsForScenario, int selectedYear)
        {
            var accountsOfPreviousYear = accountsForScenario.Where(a => a.Year == (selectedYear - 1)).ToList();
            var previousYearIsFinancialYear = (selectedYear - 1) == FinancialYear;

            foreach (var acc in accountsOfPreviousYear)
            {
                accountsForScenario.Add(
                    new AccountYearDto
                    {
                        SubjectId = acc.SubjectId,
                        IdOfParentFunctionGroup = acc.IdOfParentFunctionGroup,
                        Type = acc.Type,
                        Year = selectedYear,
                        ExpensesBudget = previousYearIsFinancialYear ? acc.ExpensesActual : acc.ExpensesBudget,
                        IncomeBudget = previousYearIsFinancialYear ? acc.IncomeActual : acc.IncomeBudget,
                    });
            }

        }

    }
}
