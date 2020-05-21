using Projekt2.DbModels;
using Projekt2.Services.Enums;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Projekt2.Constants;
using Projekt2.Models;
using Microsoft.EntityFrameworkCore;

namespace Projekt2.Services
{
    public class ScenarioServiceER
    {
        private readonly Project2Context _context;
        private readonly DataServiceER _dataSvc;

        public ScenarioServiceER(Project2Context context, DataServiceER dataSvc)
        {
            _context = context;
            _dataSvc = dataSvc;
        }


        /** Timeline ER - Fetch main-groups **/
        public MultipleYearsViewModel FetchMainGroupsForTimelineEr(Scenario scenario,
                                                                    StructureType selectedType,
                                                                    ERAccountType erAccountType,
                                                                    List<int> selectedYears,
                                                                    int selectedLevel)
        {
            if (scenario == null) {
                throw new Exception("No scenario defined!");
            }

            List<AccountYearDto> baseAccounts = GetBaseAndScenarioYears(scenario);

            // accounts contains all accounts as a flat list:
            List<AccountYearViewModel> allAccounts = GetQueryForErAccounts(baseAccounts, selectedType, selectedYears, selectedLevel).ToList();
            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;

            return _dataSvc.AssembleMultiYearsAccountModels(isFunctionGroups, erAccountType, selectedYears, allAccounts);
        }


        /** Timeline ER - Fetch SUB-groups **/
        public List<AccountMultipleYearsViewModel> FetchSubGroupsForTimelineEr(Scenario scenario,
                                                                                AccountMultipleYearsViewModel accMultiYears,
                                                                                StructureType selectedType,
                                                                                ERAccountType selectedERAccountType)
        {
            List<AccountYearDto> baseAccountYears = GetBaseAndScenarioYears(scenario);

            string selectedAccountId = accMultiYears.AccountId;
            MultipleYearsViewModel multiYearsModel = null;

            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                string idOfParentInSuperordinateStructure = accMultiYears.IdOfParentInSuperordinateStructure ?? accMultiYears.AccountId;
                int levelOfSubordinatedAccounts = accMultiYears.IdOfParentInSuperordinateStructure == null ? 1 : (accMultiYears.AccountLevel + 1);

                List<AccountYearViewModel> allAccounts = GetQueryForErSubAccountsInMixedStructure(baseAccountYears,
                                                                                                 selectedType,
                                                                                                 selectedERAccountType,
                                                                                                 idOfParentInSuperordinateStructure,
                                                                                                 accMultiYears.AccountId,
                                                                                                 levelOfSubordinatedAccounts,
                                                                                                 accMultiYears.SelectedYears)
                                                                                                 .ToList();

                bool isFunctionGroups = selectedType == StructureType.SubjectsThenFunctions;
                multiYearsModel = _dataSvc.AssembleMultiYearsAccountModels(isFunctionGroups, selectedERAccountType, accMultiYears.SelectedYears, allAccounts);

                foreach (AccountMultipleYearsViewModel acc in multiYearsModel.AccountsWithMultipleYears)
                {
                    acc.IdOfParentInSuperordinateStructure = idOfParentInSuperordinateStructure;
                }
            }
            else if (selectedType == StructureType.Functions || selectedType == StructureType.Subjects)
            {
                var query = GetQueryForErAccounts(baseAccountYears,
                                                  selectedType,
                                                  accMultiYears.SelectedYears,
                                                  (accMultiYears.AccountLevel + 1));

                // accounts contains all accounts as a flat list:
                // Pick all accounts, whose id starts with the clicked account's id
                List<AccountYearViewModel> allAccounts = query.Where(a => a.AccountId.Substring(0, accMultiYears.AccountLevel) == selectedAccountId)
                                                                    .ToList();

                bool isFunctionGroups = selectedType == StructureType.Functions;
                multiYearsModel = _dataSvc.AssembleMultiYearsAccountModels(isFunctionGroups,
                                                                            selectedERAccountType,
                                                                            accMultiYears.SelectedYears,
                                                                            allAccounts);
            }
            return multiYearsModel?.AccountsWithMultipleYears ?? null;
        }


        private IEnumerable<AccountYearViewModel> GetQueryForErAccounts(List<AccountYearDto> baseAccountYears,
                                                                        StructureType selectedType,
                                                                        List<int> selectedYears,
                                                                        int selectedLevel)
        {
            bool isSelectedAccountFunction = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;
            bool isSelectedAccountSubject = selectedType == StructureType.Subjects || selectedType == StructureType.SubjectsThenFunctions;

            var accountYears = baseAccountYears.Where(entry => selectedYears.Contains(entry.Year) &&
                                                               entry.Type == "ER")
                                            .GroupBy(ayear => new
                                            {
                                                FunctionId = isSelectedAccountFunction ? ayear.IdOfParentFunctionGroup.Substring(0, selectedLevel) : null,
                                                SubjectId = isSelectedAccountSubject ? ayear.SubjectId.Substring(0, selectedLevel) : null,
                                                ayear.Year
                                            })
                                            .Select(o => new AccountYearDto
                                            {
                                                SubjectId = o.Key.FunctionId ?? o.Key.SubjectId, // corresponds to property of groupBy condition
                                                Year = o.Key.Year,
                                                ExpensesBudget = o.Sum(x => x.ExpensesBudget),
                                                ExpensesActual = o.Sum(x => x.ExpensesActual),
                                                IncomeBudget = o.Sum(x => x.IncomeBudget),
                                                IncomeActual = o.Sum(x => x.IncomeActual)
                                            });

            var relevantType = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects ? "FG" : "ER";

            // Inner Join
            return GetQueryForInnerJoinAccountGroupWithAccountYear(relevantType, selectedLevel, accountYears);
        }


        /* param accountGroupType == 'FG', 'ER' etc. */
        private IEnumerable<AccountYearViewModel> GetQueryForInnerJoinAccountGroupWithAccountYear(string accountGroupType,
                                                                                                 int selectedLevel,
                                                                                                 IEnumerable<AccountYearDto> accountYears)
        {
            IQueryable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == accountGroupType &&
                                                                                      a.Level == selectedLevel);
            return accountYears.Join(accountGroups,
                             aYear => aYear.SubjectId,
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


        private IEnumerable<AccountYearViewModel> GetQueryForErSubAccountsInMixedStructure(List<AccountYearDto> baseAccountYears,
                                                                                          StructureType structureType,
                                                                                          ERAccountType erAccountType,
                                                                                          string idOfParentInSuperordinateStructure,
                                                                                          string idOfSelectedAccount,
                                                                                          int levelOfSubordinatedAccounts,
                                                                                          List<int> years)
        {
            // Method accepts only mixed structure-types:
            if (structureType != StructureType.SubjectsThenFunctions && structureType != StructureType.FunctionsThenSubjects)
            {
                return null;
            }

            string idOfSuperAcc = idOfParentInSuperordinateStructure;
            int levelOfSuperAcc = idOfParentInSuperordinateStructure.Length;
            bool isFunctionGroupSuperordinated = structureType == StructureType.FunctionsThenSubjects;

            IEnumerable<AccountYearDto> query = baseAccountYears.Where(a => a.Type == "ER" && years.Contains(a.Year));

            if (isFunctionGroupSuperordinated)
            {
                query = query.Where(a => a.IdOfParentFunctionGroup.Substring(0, levelOfSuperAcc) == idOfSuperAcc);

                if (levelOfSubordinatedAccounts > 1)
                {
                    query = query.Where(a => a.SubjectId.Substring(0, (levelOfSubordinatedAccounts - 1)) == idOfSelectedAccount);
                }
            }
            // If subject-accounts represent the superordinated layer:
            else
            {
                query = query.Where(a => a.SubjectId.Substring(0, levelOfSuperAcc) == idOfSuperAcc);

                if (levelOfSubordinatedAccounts > 1)
                {
                    query = query.Where(a => a.IdOfParentFunctionGroup.Substring(0, (levelOfSubordinatedAccounts - 1)) == idOfSelectedAccount);
                }
            }

            // Check if only incomes or only expenses need to be fetched:
            if (erAccountType == ERAccountType.Expenses || erAccountType == ERAccountType.Income)
            {
                string firstDigit = erAccountType == ERAccountType.Expenses ? Const.FirstDigitOfExpenses : Const.FirstDigitOfIncomes;
                query = query.Where(a => a.SubjectId.Substring(0, 1) == firstDigit);
            }

            var accountYears = query.GroupBy(ayear => new
            {
                SubjectId = isFunctionGroupSuperordinated ? ayear.SubjectId.Substring(0, levelOfSubordinatedAccounts) : null,
                FunctionId = isFunctionGroupSuperordinated == false ? ayear.IdOfParentFunctionGroup.Substring(0, levelOfSubordinatedAccounts) : null,
                ayear.Year
            })
            .Select(o => new AccountYearDto
            {
                SubjectId = isFunctionGroupSuperordinated ? o.Key.SubjectId : o.Key.FunctionId, // corresponds to property of groupBy condition
                Year = o.Key.Year, // corresponds to property of groupBy condition
                ExpensesBudget = o.Sum(x => x.ExpensesBudget),
                ExpensesActual = o.Sum(x => x.ExpensesActual),
                IncomeBudget = o.Sum(x => x.IncomeBudget),
                IncomeActual = o.Sum(x => x.IncomeActual)
            });

            string typeInAccountGroups = structureType == StructureType.SubjectsThenFunctions ? "FG" : "ER";
            return GetQueryForInnerJoinAccountGroupWithAccountYear(typeInAccountGroups, levelOfSubordinatedAccounts, accountYears);
        }



        /******************************/
         /* 
         /* Scenario specific methods
         /* 
         /****************************/

        private List<AccountYearDto> GetBaseAndScenarioYears(Scenario scenario)
        {
            int financialYear = Const.CurrentYear - 1;
            var accountsOfBaselineYear = GetErAccountsForBaselineYear(financialYear);
            var allBaseAndScenarioAccounts = scenario.AddScenarioAccounts(accountsOfBaselineYear);
            return allBaseAndScenarioAccounts;
        }

        private List<AccountYearDto> GetErAccountsForBaselineYear(int financialYear)
        {
            return _context.AccountYear.Where(entry => entry.Year == financialYear && entry.Type == "ER")
                                            .GroupBy(ayear => new
                                            {
                                                ayear.SubjectId,
                                                ayear.FunctionId,
                                                ayear.Type,
                                                ayear.Year
                                            })
                                            .Select(o => new AccountYearDto
                                            {
                                                SubjectId = o.Key.SubjectId, // corresponds to property of groupBy condition
                                                IdOfParentFunctionGroup = o.Key.FunctionId,
                                                Type = o.Key.Type,
                                                Year = o.Key.Year,
                                                ExpensesActual = o.Sum(x => x.ExpensesEffective),
                                                IncomeActual = o.Sum(x => x.IncomeEffective)
                                            })
                                            .ToList();
        }


    }
}
