using Microsoft.EntityFrameworkCore;
using Projekt2.DbModels;
using Projekt2.Services.Enums;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Projekt2.Constants;
using Projekt2.Models;
using Microsoft.EntityFrameworkCore.Internal;

namespace Projekt2.Services
{

    public class DataServiceER
    {
        private readonly Project2Context _context;
        private readonly HelpersService _helpers;

        public DataServiceER(Project2Context context, HelpersService helpers)
        {
            _context = context;
            _helpers = helpers;
        }


        public List<int> GetRelevantYears(int financialYear, ScenarioType scenarioType)
        {
            var result = new List<int>();
            int count = scenarioType == ScenarioType.InputData ? -1 : 0;
            while (count < 6)
            {
                result.Add(financialYear + count);
                count++;
            }
            return result;
        }

        public List<int> GetPreviousYears(int currentYear, int numberOfYears)
        {
            var result = new List<int>();
            int count = -1;
            while (count >= -numberOfYears)
            {
                result.Add(currentYear + count);
                count--;
            }
            return result;
        }

        public List<int> GetFutureYearsIncludingCurrent(int currentYear, int numberOfYears)
        {
            var result = new List<int>();
            int count = 0;
            while (count < numberOfYears)
            {
                result.Add(currentYear + count);
                count++;
            }
            return result;
        }


        /*************************************************************/
        /* Methods that enable the Views to fetch the desired accounts
        /*************************************************************/

        /** Yearly ER - Fetch main-groups **/
        public async Task<YearViewModel> FetchMainGroupsForYearlyER(StructureType selectedType,
                                                                    int mostRecentFinancialYear,
                                                                    int selectedYear,
                                                                    int selectedLevel)
        {
            int previousYear = selectedYear - 1;
            var years = new List<int> { previousYear, selectedYear };

            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;
            var query = GetQueryForErAccounts(isFunctionGroups, years, selectedLevel);

            List<AccountYearViewModel> allAccounts = await query.ToListAsync();
            List<AccountYearViewModel> accountsForPreviousYear = allAccounts.Where(a => a.Year == previousYear).OrderBy(a => a.AccountId).ToList();
            List<AccountYearViewModel> accountsForSelectedYear = allAccounts.Where(a => a.Year == selectedYear).OrderBy(a => a.AccountId).ToList();

            SetPercentChangesBetweenTwoYears(accountsForPreviousYear, accountsForSelectedYear, mostRecentFinancialYear, selectedYear);

            YearTotalsViewModel totalsForSelectedYear = GetTotalsForYears(years, allAccounts, mostRecentFinancialYear).FirstOrDefault(y => y.Year == selectedYear);

            return new YearViewModel
            {
                Year = selectedYear,
                Accounts = accountsForSelectedYear,
                AccountYearTotals = totalsForSelectedYear
            };
        }


        /** Yearly ER - Fetch SUB-groups **/
        public async Task<List<AccountYearViewModel>> FetchSubGroupsForYearlyEr(AccountYearViewModel ayear,
                                                                                StructureType selectedType,
                                                                                int mostRecentFinancialYear)
        {
            List<AccountYearViewModel> accountYearModel = null;

            // If user is in mixed-structureTypes mode:
            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                // If user was on top-level when he clicked on account: IdOfParentInSuperordinateStructure == null
                // If user clicked on an account below the top-level-structure: IdOfParentInSuperordinateStructure will have a value
                string idOfParentInSuperordinateStructure = ayear.IdOfParentInSuperordinateStructure ?? ayear.AccountId;
                int levelOfAccountsToFetch = ayear.IdOfParentInSuperordinateStructure == null ? 1 : (ayear.AccountLevel + 1);

                List<int> years = new List<int> { (ayear.Year - 1), ayear.Year };

                var query = GetQueryForErSubAccountsInMixedStructure(selectedType, ERAccountType.ExpensesAndIncomes, idOfParentInSuperordinateStructure, ayear.AccountId, levelOfAccountsToFetch, years);

                List<AccountYearViewModel> accountsForPreviousYear = await query.Where(a => a.Year == (ayear.Year - 1)).ToListAsync();
                List<AccountYearViewModel> accountsForSelectedYear = await query.Where(a => a.Year == ayear.Year).ToListAsync();

                SetPercentChangesBetweenTwoYears(accountsForPreviousYear,
                                                 accountsForSelectedYear,
                                                 mostRecentFinancialYear,
                                                 ayear.Year);

                // Add id of superordinate subject-account:
                foreach (var acc in accountsForSelectedYear)
                {
                    acc.IdOfParentInSuperordinateStructure = idOfParentInSuperordinateStructure;
                }
                accountYearModel = accountsForSelectedYear;
            }
            // If user is in subjects-mode or in functions-mode:
            else if (selectedType == StructureType.Functions || selectedType == StructureType.Subjects)
            {
                YearViewModel m = await FetchMainGroupsForYearlyER(selectedType,
                                                                   mostRecentFinancialYear,
                                                                   ayear.Year,
                                                                  (ayear.AccountLevel) + 1);

                accountYearModel = m.Accounts.Where(a => a.AccountId.Substring(0, ayear.AccountLevel) == ayear.AccountId).ToList();
            }
            return accountYearModel;
        }


        /** Timeline ER - Fetch main-groups **/
        public async Task<MultipleYearsViewModel> FetchMainGroupsForTimelineEr(StructureType selectedType,
                                                                               ERAccountType erAccountType,
                                                                               List<int> selectedYears,
                                                                               int mostRecentFinancialYear,
                                                                               int selectedLevel)
        {
            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;
            var query = GetQueryForErAccounts(isFunctionGroups, selectedYears, selectedLevel);

            // accounts contains all accounts as a flat list:
            List<AccountYearViewModel> allAccounts = await query.ToListAsync();

            return AssembleMultiYearsAccountModels(isFunctionGroups,
                                                   erAccountType,
                                                   selectedYears,
                                                   mostRecentFinancialYear,
                                                   allAccounts);
        }


        /** Timeline ER - Fetch SUB-groups **/
        public async Task<List<AccountMultipleYearsViewModel>> FetchSubGroupsForTimelineEr(AccountMultipleYearsViewModel accMultiYears, 
                                                                                           StructureType selectedType,
                                                                                           ERAccountType selectedERAccountType,
                                                                                           int mostRecentFinancialYear)
        {
            string selectedAccountId = accMultiYears.AccountId;
            List<AccountYearViewModel> allAccounts = null;
            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.SubjectsThenFunctions;

            string idOfParentInSuperordinateStructure = null;

            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                idOfParentInSuperordinateStructure = accMultiYears.IdOfParentInSuperordinateStructure ?? accMultiYears.AccountId;
                int levelOfSubordinatedAccounts = accMultiYears.IdOfParentInSuperordinateStructure == null ? 1 : (accMultiYears.AccountLevel + 1);

                allAccounts = await GetQueryForErSubAccountsInMixedStructure(selectedType,
                                                                             selectedERAccountType,
                                                                             idOfParentInSuperordinateStructure,
                                                                             accMultiYears.AccountId,
                                                                             levelOfSubordinatedAccounts,
                                                                             accMultiYears.SelectedYears)
                                                                             .ToListAsync();          
            }
            else if (selectedType == StructureType.Functions || selectedType == StructureType.Subjects)
            {
                var query = GetQueryForErAccounts(isFunctionGroups,
                                                 accMultiYears.SelectedYears,
                                                 (accMultiYears.AccountLevel + 1));

                // accounts contains all accounts as a flat list:
                // Pick all accounts, whose id starts with the clicked account's id
                allAccounts = await query.Where(a => a.AccountId.Substring(0, accMultiYears.AccountLevel) == selectedAccountId)
                                         .ToListAsync();
            }

            MultipleYearsViewModel multiYearsModel = AssembleMultiYearsAccountModels(isFunctionGroups,
                                                                                     selectedERAccountType,
                                                                                     accMultiYears.SelectedYears,
                                                                                     mostRecentFinancialYear,
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



        /*******************************************
         * Helpers to assemble the Database-Queries:
         ******************************************/

        private IQueryable<AccountYearViewModel> GetQueryForErAccounts(bool isFunctionGroups,
                                                                       List<int> selectedYears,
                                                                       int selectedLevel)
        {
            IQueryable<AccountYear> accountYears = _context.AccountYear.Where(entry => selectedYears.Contains(entry.Year) &&
                                                                              entry.Type == "ER");

            return GroupAccountYearsAndJoinWithAccountGroups(isFunctionGroups, accountYears, selectedLevel);
        }


        private IQueryable<AccountYearViewModel> GetQueryForErSubAccountsInMixedStructure(StructureType structureType,
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

            IQueryable<AccountYear> query = _context.AccountYear.Where(a => a.Type == "ER" && years.Contains(a.Year));

            if (isFunctionGroupSuperordinated)
            {
                query = query.Where(a => a.FunctionId.Substring(0, levelOfSuperAcc) == idOfSuperAcc);

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
                    query = query.Where(a => a.FunctionId.Substring(0, (levelOfSubordinatedAccounts - 1)) == idOfSelectedAccount);
                }
            }
            bool isFunctionGroup = structureType == StructureType.SubjectsThenFunctions;
            return GroupAccountYearsAndJoinWithAccountGroups(isFunctionGroup, query, levelOfSubordinatedAccounts);
        }


        /* param accountGroupType == 'FG', 'ER' etc. */
        private IQueryable<AccountYearViewModel> GroupAccountYearsAndJoinWithAccountGroups(bool isFunctionGroups,
                                                                                           IQueryable<AccountYear> accountYears,
                                                                                           int selectedLevel)
        {
            var groupedAccYears = accountYears.GroupBy(accYear => new
            {
                FunctionId = isFunctionGroups ? accYear.FunctionId.Substring(0, selectedLevel) : null,
                SubjectId = isFunctionGroups == false ? accYear.SubjectId.Substring(0, selectedLevel) : null,
                accYear.Year
            })
            .Select(o => new AccountYearDto
            {
                SubjectId = o.Key.FunctionId ?? o.Key.SubjectId, // corresponds to property of groupBy condition
                Year = o.Key.Year,
                ExpensesBudget = o.Sum(x => x.ExpensesBudget),
                ExpensesActual = o.Sum(x => x.ExpensesEffective),
                IncomeBudget = o.Sum(x => x.IncomeBudget),
                IncomeActual = o.Sum(x => x.IncomeEffective)
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



        /*******************************************
         * Helpers to assemble the final ViewModels
         ******************************************/

        private void SetPercentChangesBetweenTwoYears(List<AccountYearViewModel> accountsForPreviousYear,
                                                      List<AccountYearViewModel> accountsForSelectedYear,
                                                      int mostRecentFinancialYear,
                                                      int selectedYear)
        {
            foreach (AccountYearViewModel acc in accountsForSelectedYear)
            {
                // For account-group-items which had no partner in LEFT-JOIN the year was not yet set:
                acc.Year = selectedYear;

                AccountYearViewModel accInPrevYear = accountsForPreviousYear.FirstOrDefault(a => a.AccountId == acc.AccountId);

                if (accInPrevYear != null)
                {
                    SetPercentChangesBetweenTwoYears(accInPrevYear, acc, mostRecentFinancialYear, selectedYear);
                }
            }
        }


        /* Assembles models that contain the values of an account for multiple years. */
        internal MultipleYearsViewModel AssembleMultiYearsAccountModels(bool isFunctionGroups,
                                                                        ERAccountType erAccountType,
                                                                        List<int> selectedYears,
                                                                        int mostRecentFinancialYear,
                                                                        List<AccountYearViewModel> allAccounts)
        {
            List<string> accountIds = new List<string>();

            // In case the incoming accounts are function-groups
            if (isFunctionGroups)
            {
                accountIds = allAccounts.Select(a => a.AccountId)
                                        .OrderBy(a => a)
                                        .Distinct()
                                        .ToList();
            }
            // In case the incoming accounts are subject-groups
            else
            {
                List<string> allowedFirstDigits = new List<string>();

                if (erAccountType == ERAccountType.Expenses || erAccountType == ERAccountType.Balances) 
                {
                    allowedFirstDigits.Add(Const.FirstDigitOfExpenses);
                }
                if (erAccountType == ERAccountType.Income || erAccountType == ERAccountType.Balances)
                {
                    allowedFirstDigits.Add(Const.FirstDigitOfIncomes);
                }

                // all "Aufwand"-accounts resp. "Ertrag"-accounts start with the same number => use this fact for filtering:
                accountIds = allAccounts.Where(a => allowedFirstDigits.Contains(a.AccountId.Substring(0, 1)))
                                        .Select(a => a.AccountId)
                                        .OrderBy(a => a)
                                        .Distinct()
                                        .ToList();
            }

            List<YearTotalsViewModel> totalsForSelectedYears = GetTotalsForYears(selectedYears, allAccounts, mostRecentFinancialYear);

            var result = new MultipleYearsViewModel
            {
                SelectedYears = selectedYears,
                ListOfAccountYearTotals = totalsForSelectedYears,
                AccountsWithMultipleYears = new List<AccountMultipleYearsViewModel>()
            };

            /* There might be account-ids which are only available in certain years:
               For these cases*/

            foreach (var id in accountIds)
            {
                var yearlyAccounts = new List<AccountYearViewModel>();
                for (int i = 0; i < selectedYears.Count; i++)
                {
                    // Check if an account for the given year / id is available
                    AccountYearViewModel yearlyAcc = allAccounts.FirstOrDefault(a => a.AccountId == id &&
                                                                                     a.Year == selectedYears[i]);

                    if (yearlyAcc != null)
                    {
                        yearlyAccounts.Add(yearlyAcc);
                    }
                    // If for the given year no account with this id is available, 
                    // create a new account-object based on the infos of another account with the same id (setting expenses and incomes == 0)
                    else
                    {
                        var ya = allAccounts.FirstOrDefault(a => a.AccountId == id);
                        if (ya != null)
                        {
                            yearlyAccounts.Add(new AccountYearViewModel
                            {
                                Year = selectedYears[i],
                                AccountId = ya.AccountId,
                                AccountName = ya.AccountName,
                                AccountLevel = ya.AccountLevel,
                                ParentId = ya.ParentId,
                                Type = ya.Type
                            });
                        }
                    }

                    // Given there is a previous year,
                    // add percent change compared to previous year:
                    if (i > 0)
                    {
                        SetPercentChangesBetweenTwoYears(yearlyAccounts[i - 1], yearlyAccounts[i], mostRecentFinancialYear, selectedYears[i]);
                    }
                }

                result.AccountsWithMultipleYears.Add(new AccountMultipleYearsViewModel
                {
                    Type = yearlyAccounts[0].Type,
                    AccountId = yearlyAccounts[0].AccountId,
                    AccountName = yearlyAccounts[0].AccountName,
                    AccountLevel = yearlyAccounts[0].AccountLevel,
                    ParentId = yearlyAccounts[0].ParentId,
                    SelectedYears = selectedYears,
                    YearlyAccounts = yearlyAccounts
                });
            }
            return result;
        }


        private void SetPercentChangesBetweenTwoYears(AccountYearViewModel accPreviousYear,
                                                      AccountYearViewModel accSelectedYear,
                                                      int mostRecentFinancialYear,
                                                      int selectedYear)
        {
            if (selectedYear <= mostRecentFinancialYear)
            {
                accSelectedYear.PercentageChangeExpensesActual = _helpers.GetPercentageChange(accPreviousYear.ExpensesActual, accSelectedYear.ExpensesActual);
                accSelectedYear.PercentageChangeIncomeActual = _helpers.GetPercentageChange(accPreviousYear.IncomeActual, accSelectedYear.IncomeActual);
                accSelectedYear.PercentageChangeBalanceActual = _helpers.GetPercentageChange(accPreviousYear.BalanceActual, accSelectedYear.BalanceActual);
            }
            else if (selectedYear == (mostRecentFinancialYear + 1))
            {
                accSelectedYear.PercentageChangeExpensesBudget = _helpers.GetPercentageChange(accPreviousYear.ExpensesActual, accSelectedYear.ExpensesBudget);
                accSelectedYear.PercentageChangeIncomeBudget = _helpers.GetPercentageChange(accPreviousYear.IncomeActual, accSelectedYear.IncomeBudget);
                accSelectedYear.PercentageChangeBalanceBudget = _helpers.GetPercentageChange(accPreviousYear.BalanceActual, accSelectedYear.BalanceBudget);

            }
            else if (selectedYear > (mostRecentFinancialYear + 1))
            {
                accSelectedYear.PercentageChangeExpensesBudget = _helpers.GetPercentageChange(accPreviousYear.ExpensesBudget, accSelectedYear.ExpensesBudget);
                accSelectedYear.PercentageChangeIncomeBudget = _helpers.GetPercentageChange(accPreviousYear.IncomeBudget, accSelectedYear.IncomeBudget);
                accSelectedYear.PercentageChangeBalanceBudget = _helpers.GetPercentageChange(accPreviousYear.BalanceBudget, accSelectedYear.BalanceBudget);
            }
        }


        private List<YearTotalsViewModel> GetTotalsForYears(List<int> years,
                                                            List<AccountYearViewModel> allAccounts,
                                                            int mostRecentFinancialYear)
        {
            List<YearTotalsViewModel> totalsForSelectedYears = new List<YearTotalsViewModel>();

            for (int i = 0; i < years.Count; i++)
            {
                var accSelectedY = allAccounts.Where(a => a.Year == years[i]).ToList();
                bool hasAcc = accSelectedY.Any();

                YearTotalsViewModel ts = new YearTotalsViewModel { Year = years[i] };

                if (hasAcc)
                {
                    ts.ExpensesActualTotal = accSelectedY.Where(a => a.Year == years[i]).Select(a => a.ExpensesActual).Sum();
                    ts.ExpensesBudgetTotal = accSelectedY.Where(a => a.Year == years[i]).Select(a => a.ExpensesBudget).Sum();
                    ts.IncomeActualTotal = accSelectedY.Where(a => a.Year == years[i]).Select(a => a.IncomeActual).Sum();
                    ts.IncomeBudgetTotal = accSelectedY.Where(a => a.Year == years[i]).Select(a => a.IncomeBudget).Sum();
                    ts.BalanceActualTotal = (ts.IncomeActualTotal ?? 0) - (ts.ExpensesActualTotal ?? 0);
                    ts.BalanceBudgetTotal = (ts.IncomeBudgetTotal ?? 0) - (ts.ExpensesBudgetTotal ?? 0);
                }
                else
                {
                    totalsForSelectedYears.Add(ts);
                    continue;
                }

                // If a previous year exists and contains at least one account, calculate percentage of changes
                if (i > 0 && allAccounts.Any(a => a.Year == years[i - 1]))
                {
                    // Get totals of previous year
                    YearTotalsViewModel totalsOfPY = totalsForSelectedYears[i - 1];
                    ts.HasPreviousYear = true;

                    if (years[i] <= mostRecentFinancialYear)
                    {
                        ts.PercentageChangeExpensesActualTotal = _helpers.GetPercentageChange(totalsOfPY.ExpensesActualTotal, ts.ExpensesActualTotal);
                        ts.PercentageChangeIncomeActualTotal = _helpers.GetPercentageChange(totalsOfPY.IncomeActualTotal, ts.IncomeActualTotal);
                        ts.PercentageChangeBalanceActualTotal = _helpers.GetPercentageChange(totalsOfPY.BalanceActualTotal, ts.BalanceActualTotal);
                    }
                    else if (years[i] == (mostRecentFinancialYear + 1))
                    {
                        ts.PercentageChangeExpensesBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.ExpensesActualTotal, ts.ExpensesBudgetTotal);
                        ts.PercentageChangeIncomeBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.IncomeActualTotal, ts.IncomeBudgetTotal);
                        ts.PercentageChangeBalanceBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.BalanceActualTotal, ts.BalanceBudgetTotal);
                    }
                    else if (years[i] > (mostRecentFinancialYear + 1))
                    {
                        ts.PercentageChangeExpensesBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.ExpensesBudgetTotal, ts.ExpensesBudgetTotal);
                        ts.PercentageChangeIncomeBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.IncomeBudgetTotal, ts.IncomeBudgetTotal);
                        ts.PercentageChangeBalanceBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.BalanceBudgetTotal, ts.BalanceBudgetTotal);
                    }
                }

                totalsForSelectedYears.Add(ts);
            }
            return totalsForSelectedYears;
        }


    }
}
