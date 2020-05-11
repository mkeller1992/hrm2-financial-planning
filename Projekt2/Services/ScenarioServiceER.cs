using Projekt2.DbModels;
using Projekt2.Services.Enums;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Projekt2.Constants;
using Projekt2.Models;

namespace Projekt2.Services
{
    public class ScenarioServiceER
    {
        private readonly Project2Context _context;
        private readonly DataServiceER _dataSvc;

        private List<AccountYear> _accountsOfCurrentScenario;
        private Scenario _currentScenario;

        public ScenarioServiceER(Project2Context context, DataServiceER dataSvc) 
        {
            _context = context;
            _dataSvc = dataSvc;
        }



        /** Timeline ER - Fetch main-groups **/
        public async Task<MultipleYearsViewModel> FetchMainGroupsForTimelineEr(StructureType selectedType, ERAccountType erAccountType, List<int> selectedYears, int selectedLevel, bool allExistingAccounts)
        {
            // accounts contains all accounts as a flat list:
            List<AccountYearViewModel> allAccounts = GetListWithErAccounts(selectedType, selectedYears, selectedLevel, allExistingAccounts).ToList();

            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;

            return _dataSvc.AssembleMultiYearsAccountModels(isFunctionGroups, erAccountType, selectedYears, allAccounts);
        }

        private IEnumerable<AccountYearViewModel> GetListWithErAccounts(StructureType selectedType,
                                                                              List<int> selectedYears,
                                                                              int selectedLevel,
                                                                              bool allExistingAccounts)
        {
            bool isSelectedAccountFunction = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;
            bool isSelectedAccountSubject = selectedType == StructureType.Subjects || selectedType == StructureType.SubjectsThenFunctions;

            var accountYears = _context.AccountYear.Where(entry => selectedYears.Contains(entry.Year) &&
                                                                   entry.Type == "ER")
                                            .GroupBy(ayear => new
                                            {
                                                FunctionId = isSelectedAccountFunction ? ayear.FunctionId.Substring(0, selectedLevel) : null,
                                                SubjectId = isSelectedAccountSubject ? ayear.SubjectId.Substring(0, selectedLevel) : null,
                                                ayear.Year
                                            })
                                            .Select(o => new AccountYearDto
                                            {
                                                Id = o.Key.FunctionId ?? o.Key.SubjectId, // corresponds to property of groupBy condition
                                                Year = o.Key.Year,
                                                ExpensesBudget = o.Sum(x => x.ExpensesBudget),
                                                ExpensesActual = o.Sum(x => x.ExpensesEffective),
                                                IncomeBudget = o.Sum(x => x.IncomeBudget),
                                                IncomeActual = o.Sum(x => x.IncomeEffective)
                                            });

            var relevantType = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects ? "FG" : "ER";

            // In case user wants to see all accounts, including the unused:
            if (allExistingAccounts)
            {
                IEnumerable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == relevantType &&
                                                                         a.Level == selectedLevel);

                // Do LEFT JOIN
                return from agroup in accountGroups
                       join ayear in accountYears on agroup.Id equals ayear.Id into gj
                       from subAYear in gj.DefaultIfEmpty()
                       select new AccountYearViewModel
                       {
                           Type = agroup.Type,
                           AccountId = agroup.Id,
                           AccountName = agroup.Name,
                           AccountLevel = agroup.Level,
                           ParentId = agroup.ParentId,
                           Year = subAYear.Year,
                           ExpensesBudget = subAYear.ExpensesBudget,
                           ExpensesActual = subAYear.ExpensesActual,
                           IncomeBudget = subAYear.IncomeBudget,
                           IncomeActual = subAYear.IncomeActual,
                           BalanceActual = (subAYear.IncomeActual ?? 0) - (subAYear.ExpensesActual ?? 0),
                           BalanceBudget = (subAYear.IncomeBudget ?? 0) - (subAYear.ExpensesBudget ?? 0)
                       };
            }
            // In case user wants to see just the used accounts:
            else
            {
                // Inner Join
                return GetQueryForInnerJoinAccountGroupWithAccountYear(relevantType, selectedLevel, accountYears);
            }
        }

        /* param accountGroupType == 'FG', 'ER' etc. */
        private IEnumerable<AccountYearViewModel> GetQueryForInnerJoinAccountGroupWithAccountYear(string accountGroupType,
                                                                                                 int selectedLevel,
                                                                                                 IEnumerable<AccountYearDto> accountYears)
        {
            IEnumerable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == accountGroupType &&
                                                                                 a.Level == selectedLevel);

            return accountYears.Join(accountGroups,
                             aYear => aYear.Id,
                             aGroup => aGroup.Id, (aYear, aGroup)
                             => new AccountYearViewModel
                             {
                                 Type = aGroup.Type,
                                 AccountId = aGroup.Id,
                                 AccountName = aGroup.Name,
                                 AccountLevel = aGroup.Level,
                                 ParentId = aGroup.ParentId,
                                 Year = aYear.Year,
                                 ExpensesBudget = aYear.ExpensesBudget,
                                 ExpensesActual = aYear.ExpensesActual,
                                 IncomeBudget = aYear.IncomeBudget,
                                 IncomeActual = aYear.IncomeActual,
                                 BalanceActual = (aYear.IncomeActual ?? 0) - (aYear.ExpensesActual ?? 0),
                                 BalanceBudget = (aYear.IncomeBudget ?? 0) - (aYear.ExpensesBudget ?? 0)
                             });
        }




    }
}
