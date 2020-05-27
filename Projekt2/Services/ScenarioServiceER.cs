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


        /*****************************************************/
        /* Method to compute the scenario-accounts on level 4
        /*****************************************************/

        private List<AccountYearDto> GetBaseAndScenarioYears(Scenario scenario)
        {
            int financialYear = scenario.FinancialYear;

            // Get values for base-accounts from database:
            var accountsOfBaselineYear = _context.AccountYear.Where(entry => entry.Year == financialYear &&
                                                                             entry.Type == "ER")
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

            // For each base-account the corresponding future accounts will be added + the scenario-parameters will be applied:
            var allBaseAndScenarioAccounts = scenario.ExtendBaseYearWithComputedScenarioYears(accountsOfBaselineYear);
            return allBaseAndScenarioAccounts;
        }


        /*************************************************************/
        /* Methods that enable the Views to fetch the desired accounts
        /*************************************************************/

        /** Timeline ER - Fetch main-groups **/
        public MultipleYearsViewModel FetchMainGroupsForTimelineEr(Scenario scenario,
                                                                    StructureType selectedType,
                                                                    ERAccountType erAccountType,
                                                                    List<int> selectedYears,
                                                                    int selectedLevel)
        {
            if (scenario == null)
            {
                throw new Exception("No scenario defined!");
            }

            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;

            // Get all accounts of level 4 including changes based on scenario:
            List<AccountYearDto> allBaseAndScenarioAccounts = GetBaseAndScenarioYears(scenario);

            // accounts contains all accounts as a flat list:
            List<AccountYearViewModel> allAccounts = GroupAccountYearsAndJoinWithAccountGroups(isFunctionGroups,
                                                                                               allBaseAndScenarioAccounts,
                                                                                               selectedLevel)
                                                                                               .ToList();
            return _dataSvc.AssembleMultiYearsAccountModels(isFunctionGroups,
                                                            erAccountType,
                                                            selectedYears,
                                                            scenario.FinancialYear,
                                                            allAccounts);
        }


        /** Timeline ER - Fetch SUB-groups **/
        public List<AccountMultipleYearsViewModel> FetchSubGroupsForTimelineEr(Scenario scenario,
                                                                               AccountMultipleYearsViewModel accMultiYears,
                                                                               StructureType selectedType,
                                                                               ERAccountType selectedERAccountType)
        {

            List<AccountYearDto> allBaseAndScenarioAccounts = GetBaseAndScenarioYears(scenario);

            string selectedAccountId = accMultiYears.AccountId;
            List<AccountYearViewModel> allAccounts = null;
            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.SubjectsThenFunctions;

            string idOfParentInSuperordinateStructure = null;

            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                idOfParentInSuperordinateStructure = accMultiYears.IdOfParentInSuperordinateStructure ?? accMultiYears.AccountId;
                int levelOfSubordinatedAccounts = accMultiYears.IdOfParentInSuperordinateStructure == null ? 1 : (accMultiYears.AccountLevel + 1);

                allAccounts = GetEnumerableForErSubAccountsInMixedStructure(allBaseAndScenarioAccounts,
                                                                       selectedType,
                                                                       selectedERAccountType,
                                                                       idOfParentInSuperordinateStructure,
                                                                       accMultiYears.AccountId,
                                                                       levelOfSubordinatedAccounts,
                                                                       accMultiYears.SelectedYears)
                                                                       .ToList();
            }
            else if (selectedType == StructureType.Functions || selectedType == StructureType.Subjects)
            {
                var query = GroupAccountYearsAndJoinWithAccountGroups(isFunctionGroups,
                                                                      allBaseAndScenarioAccounts,
                                                                     (accMultiYears.AccountLevel + 1));

                // accounts contains all accounts as a flat list:
                // Pick all accounts, whose id starts with the clicked account's id
                allAccounts = query.Where(a => a.AccountId.Substring(0, accMultiYears.AccountLevel) == selectedAccountId)
                                   .ToList();
            }

            MultipleYearsViewModel multiYearsModel = _dataSvc.AssembleMultiYearsAccountModels(isFunctionGroups,
                                                                                     selectedERAccountType,
                                                                                     accMultiYears.SelectedYears,
                                                                                     scenario.FinancialYear,
                                                                                     allAccounts);

            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                foreach (AccountMultipleYearsViewModel acc in multiYearsModel.AccountsWithMultipleYears)
                {
                    acc.IdOfParentInSuperordinateStructure = idOfParentInSuperordinateStructure;
                }
            }
            return multiYearsModel?.AccountsWithMultipleYears ?? null;
        }


        /*************************************
         * Helpers to assemble the Enumerables
         *************************************/

        private IEnumerable<AccountYearViewModel> GetEnumerableForErSubAccountsInMixedStructure(List<AccountYearDto> allBaseAndScenarioAccounts,
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

            IEnumerable<AccountYearDto> query = allBaseAndScenarioAccounts.Where(a => a.Type == "ER" && years.Contains(a.Year));

            if (isFunctionGroupSuperordinated)
            {
                query = query.Where(a => a.IdOfParentFunctionGroup.Substring(0, levelOfSuperAcc) == idOfSuperAcc);

                // if subordinated subject-accounts are on level 1 => if all "Aufwände" or all "Erträge" have to be fetched:
                // => all subject-ids starting with 3 vs. all subject-ids starting with 4
                if (levelOfSubordinatedAccounts == 1)
                {
                    string firstDigit = erAccountType == ERAccountType.Expenses ? Const.FirstDigitOfExpenses : Const.FirstDigitOfIncomes;
                    query = query.Where(a => a.SubjectId.Substring(0, 1) == firstDigit);
                }
                // if subordinated subject-accounts are on level 2, 3 or 4 (e.g. subject-groups: 42, 421, 4210)
                else
                {
                    query = query.Where(a => a.SubjectId.Substring(0, (levelOfSubordinatedAccounts - 1)) == idOfSelectedAccount);
                }
            }
            // If subject-accounts represent the superordinated layer:
            else
            {
                query = query.Where(a => a.SubjectId.Substring(0, levelOfSuperAcc) == idOfSuperAcc);

                // if subordinated function-accounts are on level 2, 3 or 4 (e.g. function-groups: 10, 101, 1012)
                if (levelOfSubordinatedAccounts > 1)
                {
                    query = query.Where(a => a.IdOfParentFunctionGroup.Substring(0, (levelOfSubordinatedAccounts - 1)) == idOfSelectedAccount);
                }
            }
            bool isFunctionGroup = structureType == StructureType.SubjectsThenFunctions;
            return GroupAccountYearsAndJoinWithAccountGroups(isFunctionGroup, query, levelOfSubordinatedAccounts);
        }


        private IEnumerable<AccountYearViewModel> GroupAccountYearsAndJoinWithAccountGroups(bool isFunctionGroups,
                                                                                            IEnumerable<AccountYearDto> accountYears,
                                                                                            int selectedLevel)
        {
            var groupedAccYears = accountYears.GroupBy(accYear => new
            {
                FunctionId = isFunctionGroups ? accYear.IdOfParentFunctionGroup.Substring(0, selectedLevel) : null,
                SubjectId = isFunctionGroups == false ? accYear.SubjectId.Substring(0, selectedLevel) : null,
                accYear.Year
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

            var accountGroupType = isFunctionGroups ? "FG" : "ER";

            IQueryable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == accountGroupType &&
                                                                                      a.Level == selectedLevel);

            return groupedAccYears.Join(accountGroups,
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


    }
}
