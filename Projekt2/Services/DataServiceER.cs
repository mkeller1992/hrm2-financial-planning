using Microsoft.EntityFrameworkCore;
using Projekt2.DbModels;
using Projekt2.Services.Enums;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Projekt2.Dtos;

namespace Projekt2.Services
{

    public class DataServiceER
    {
        private readonly Project2Context _context;
        private readonly HelpersService _helpers;

        public readonly int CurrentYear = 2017;

        public DataServiceER(Project2Context context, HelpersService helpers)
        {
            _context = context;
            _helpers = helpers;
        }


        public List<int> GetRelevantYears(int currentYear)
        {
            var result = new List<int>();
            int count = -2;
            while (count < 5)
            {
                result.Add((currentYear + count));
                count++;
            }
            return result;
        }


        /** Yearly ER - Fetch main-groups **/
        public async Task<YearViewModel> FetchMainGroupsForYearlyER(StructureType selectedType, int selectedYear, int selectedLevel, bool allExistingAccounts)
        {
            int previousYear = selectedYear - 1;
            var years = new List<int> { previousYear, selectedYear };

            var query = GetQueryForErAccountsInPlainStructure(selectedType, years, selectedLevel, allExistingAccounts);

            List<AccountYearViewModel> allAccounts = await query.ToListAsync();
            List<AccountYearViewModel> accountsForPreviousYear = allAccounts.Where(a => a.Year == previousYear).OrderBy(a => a.AccountId).ToList();
            List<AccountYearViewModel> accountsForSelectedYear = allAccounts.Where(a => a.Year == selectedYear).OrderBy(a => a.AccountId).ToList();

            SetPercentChangesBetweenTwoYears(accountsForPreviousYear, accountsForSelectedYear, selectedYear);

            YearTotalsViewModel totalsForSelectedYear = GetTotalsForYears(years, allAccounts).FirstOrDefault(y => y.Year == selectedYear);

            return new YearViewModel
            {
                Year = selectedYear,
                Accounts = accountsForSelectedYear,
                AccountYearTotals = totalsForSelectedYear
            };
        }


        /** Yearly ER - Fetch SUB-groups **/
        public async Task<List<AccountYearViewModel>> FetchSubGroupsForYearlyEr(AccountYearViewModel ayear, StructureType selectedType, bool allExistingAccounts)
        {
            if (ayear.AccountLevel >= 4)
            {
                return null;
            }

            // If user is in mixed-structureTypes mode:
            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                // If user was on top-level when he clicked on account: IdOfParentInSuperordinateStructure == null
                // If user clicked on an account below the top-level-structure: IdOfParentInSuperordinateStructure will have a value

                string idOfParentInSuperordinateStructure = ayear.IdOfParentInSuperordinateStructure ?? ayear.AccountId;
                int levelOfAccountsToFetch = ayear.IdOfParentInSuperordinateStructure == null ? 1 : (ayear.AccountLevel + 1);

                return await FetchSubGroupsInMixedStructureInYearlyER(selectedType,
                                                                     ERAccountType.ExpensesAndIncomes,
                                                                        idOfParentInSuperordinateStructure,
                                                                        ayear.AccountId,
                                                                        levelOfAccountsToFetch,
                                                                        ayear.Year);
            }

            // If user is in subjects-mode or in functions-mode:
            YearViewModel m = await FetchMainGroupsForYearlyER(selectedType, ayear.Year, (ayear.AccountLevel) + 1, allExistingAccounts);
            return m.Accounts.Where(a => a.AccountId.Substring(0, ayear.AccountLevel) == ayear.AccountId).ToList();
        }



        /* Fetch sub-groups if selected mode is 'function-accounts with underlying subject-accounts' */
        private async Task<List<AccountYearViewModel>> FetchSubGroupsInMixedStructureInYearlyER(StructureType structureType,
                                                                                                ERAccountType erAccountType,
                                                                                                   string idOfParentInSuperordinateStructure,
                                                                                                   string idOfSelectedAccount,
                                                                                                   int levelOfSubordinatedAccounts,
                                                                                                   int selectedYear)
        {
            int previousYear = selectedYear - 1;
            List<int> years = new List<int> { previousYear, selectedYear };

            var query = GetQueryForErSubAccountsInMixedStructure(structureType, erAccountType, idOfParentInSuperordinateStructure, idOfSelectedAccount, levelOfSubordinatedAccounts, years);

            List<AccountYearViewModel> accountsForPreviousYear = await query.Where(a => a.Year == previousYear).ToListAsync();
            List<AccountYearViewModel> accountsForSelectedYear = await query.Where(a => a.Year == selectedYear).ToListAsync();

            SetPercentChangesBetweenTwoYears(accountsForPreviousYear, accountsForSelectedYear, selectedYear);

            // Add id of superordinate subject-account:
            foreach (var acc in accountsForSelectedYear)
            {
                acc.IdOfParentInSuperordinateStructure = idOfParentInSuperordinateStructure;
            }

            return accountsForSelectedYear;
        }


        /** Timeline ER - Fetch main-groups **/
        public async Task<MultipleYearsViewModel> FetchMainGroupsForTimelineEr(StructureType selectedType, ERAccountType erAccountType, List<int> selectedYears, int selectedLevel, bool allExistingAccounts)
        {
            var query = GetQueryForErAccountsInPlainStructure(selectedType, selectedYears, selectedLevel, allExistingAccounts);

            // accounts contains all accounts as a flat list:
            List<AccountYearViewModel> allAccounts = await query.ToListAsync();
            bool isFunctionGroups = selectedType == StructureType.Functions || selectedType == StructureType.FunctionsThenSubjects;

            return AssembleMultiYearsAccountModels(isFunctionGroups, erAccountType, selectedYears, allAccounts);
        }


        /** Timeline ER - Fetch SUB-groups **/
        public async Task<List<AccountMultipleYearsViewModel>> FetchSubGroupsForTimelineEr(AccountMultipleYearsViewModel accMultiYears, 
                                                                                           StructureType selectedType,
                                                                                           ERAccountType selectedERAccountType,
                                                                                           bool allExistingAccounts)
        {
            if (accMultiYears.AccountLevel >= 4)
            {
                return null;
            }

            string selectedAccountId = accMultiYears.AccountId;
            MultipleYearsViewModel multiYearsModel = null;

            if (selectedType == StructureType.SubjectsThenFunctions || selectedType == StructureType.FunctionsThenSubjects)
            {
                string idOfParentInSuperordinateStructure = accMultiYears.IdOfParentInSuperordinateStructure ?? accMultiYears.AccountId;
                int levelOfSubordinatedAccounts = accMultiYears.IdOfParentInSuperordinateStructure == null ? 1 : (accMultiYears.AccountLevel + 1);

                List<AccountYearViewModel> allAccounts = await GetQueryForErSubAccountsInMixedStructure(selectedType,
                                                                                         selectedERAccountType,
                                                                                         idOfParentInSuperordinateStructure,
                                                                                         accMultiYears.AccountId,
                                                                                         levelOfSubordinatedAccounts,
                                                                                         accMultiYears.SelectedYears)
                                                                                         .ToListAsync();

                bool isFunctionGroups = selectedType == StructureType.SubjectsThenFunctions;
                multiYearsModel = AssembleMultiYearsAccountModels(isFunctionGroups, selectedERAccountType, accMultiYears.SelectedYears, allAccounts);                

                foreach (AccountMultipleYearsViewModel acc in multiYearsModel.AccountsWithMultipleYears)
                {
                    acc.IdOfParentInSuperordinateStructure = idOfParentInSuperordinateStructure;
                }

            }
            else if (selectedType == StructureType.Functions || selectedType == StructureType.Subjects)
            {
                var query = GetQueryForErAccountsInPlainStructure(selectedType, accMultiYears.SelectedYears, (accMultiYears.AccountLevel + 1), allExistingAccounts);

                // accounts contains all accounts as a flat list:
                // Pick all accounts, whose id starts with the clicked account's id
                List<AccountYearViewModel> allAccounts = await query.Where(a => a.AccountId.Substring(0, accMultiYears.AccountLevel) == selectedAccountId)
                                                                    .ToListAsync();

                bool isFunctionGroups = selectedType == StructureType.Functions;
                multiYearsModel = AssembleMultiYearsAccountModels(isFunctionGroups, selectedERAccountType, accMultiYears.SelectedYears, allAccounts);
            }

            return multiYearsModel?.AccountsWithMultipleYears ?? null;
        }


        private IQueryable<AccountYearViewModel> GetQueryForErAccountsInPlainStructure(StructureType selectedType,
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
                IQueryable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == relevantType &&
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
                    query = query.Where(a => a.FunctionId.Substring(0, (levelOfSubordinatedAccounts - 1)) == idOfSelectedAccount);
                }
            }

            // Check if only incomes or only expenses need to be fetched:
            if (erAccountType == ERAccountType.Expenses || erAccountType == ERAccountType.Income)
            {
                string firstDigit = erAccountType == ERAccountType.Expenses ? Constants.Constants.FirstDigitOfExpenses : Constants.Constants.FirstDigitOfIncomes;
                query = query.Where(a => a.SubjectId.Substring(0, 1) == firstDigit);
            }

            var accountYears = query.GroupBy(ayear => new
                                            {
                                                SubjectId = isFunctionGroupSuperordinated ? ayear.SubjectId.Substring(0, levelOfSubordinatedAccounts) : null,
                                                FunctionId = isFunctionGroupSuperordinated == false ? ayear.FunctionId.Substring(0, levelOfSubordinatedAccounts) : null,
                                                ayear.Year
                                            })
                                            .Select(o => new AccountYearDto
                                            {
                                                Id = isFunctionGroupSuperordinated ? o.Key.SubjectId : o.Key.FunctionId, // corresponds to property of groupBy condition
                                                Year = o.Key.Year, // corresponds to property of groupBy condition
                                                ExpensesBudget = o.Sum(x => x.ExpensesBudget),
                                                ExpensesActual = o.Sum(x => x.ExpensesEffective),
                                                IncomeBudget = o.Sum(x => x.IncomeBudget),
                                                IncomeActual = o.Sum(x => x.IncomeEffective)
                                            });

            string typeInAccountGroups = structureType == StructureType.SubjectsThenFunctions ? "FG" : "ER";
            return GetQueryForInnerJoinAccountGroupWithAccountYear(typeInAccountGroups, levelOfSubordinatedAccounts, accountYears);
        }


        /* param financialStatementType == 'FG', 'ER' etc. */
        private IQueryable<AccountYearViewModel> GetQueryForInnerJoinAccountGroupWithAccountYear(string accountGroupType,
                                                                                                 int selectedLevel,
                                                                                                 IQueryable<AccountYearDto> accountYears)
        {
            IQueryable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == accountGroupType &&
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


        private void SetPercentChangesBetweenTwoYears(List<AccountYearViewModel> accountsForPreviousYear,
                                                      List<AccountYearViewModel> accountsForSelectedYear,
                                                      int selectedYear)
        {

            foreach (AccountYearViewModel acc in accountsForSelectedYear)
            {
                // For account-group-items which had no partner in LEFT-JOIN the year was not yet set:
                acc.Year = selectedYear;

                AccountYearViewModel accInPrevYear = accountsForPreviousYear.FirstOrDefault(a => a.AccountId == acc.AccountId);

                if (accInPrevYear != null)
                {
                    SetPercentChangesBetweenTwoYears(accInPrevYear, acc, selectedYear);
                }
            }
        }


        /* Assembles models that contain the values of an account for multiple years. */
        private MultipleYearsViewModel AssembleMultiYearsAccountModels(bool isFunctionGroups,
                                                                       ERAccountType erAccountType,
                                                                       List<int> selectedYears,
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
                // all "Aufwand"-accounts resp. "Ertrag"-accounts start with the same number => use this fact for filtering:
                string firstDigit = erAccountType == ERAccountType.Expenses ? Constants.Constants.FirstDigitOfExpenses : Constants.Constants.FirstDigitOfIncomes;
                accountIds = allAccounts.Where(a => a.AccountId.Substring(0, 1) == firstDigit)
                                     .Select(a => a.AccountId)
                                     .OrderBy(a => a)
                                     .Distinct()
                                     .ToList();
            }

            List<YearTotalsViewModel> totalsForSelectedYears = GetTotalsForYears(selectedYears, allAccounts);

            var result = new MultipleYearsViewModel
            {
                SelectedYears = selectedYears,
                ListOfAccountYearTotals = totalsForSelectedYears,
                AccountsWithMultipleYears = new List<AccountMultipleYearsViewModel>()
            };

            foreach (var id in accountIds)
            {
                var yearlyAccounts = new List<AccountYearViewModel>();
                for (int i = 0; i < selectedYears.Count; i++)
                {
                    // Check if an account for the given year / id is available
                    var yearlyAcc = allAccounts.Where(a => a.AccountId == id && a.Year == selectedYears[i]).ToList();

                    // If for the given year no account with this id is available, 
                    // create a new account-object based on the infos of another account with the same id (setting expenses and incomes == 0)
                    if (yearlyAcc.Count == 0)
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
                    else if (yearlyAcc.Count == 1)
                    {
                        yearlyAccounts.Add(yearlyAcc[0]);
                    }
                    else
                    {
                        throw new Exception("Too many matching accounts!");
                    }

                    // Given there is a previous year,
                    // add percent change compared to previous year:
                    if (i > 0)
                    {
                        SetPercentChangesBetweenTwoYears(yearlyAccounts[i - 1], yearlyAccounts[i], selectedYears[i]);
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
                                                      int selectedYear)
        {
            if (selectedYear < CurrentYear)
            {
                accSelectedYear.PercentageChangeExpensesActual = _helpers.GetPercentageChange(accPreviousYear.ExpensesActual, accSelectedYear.ExpensesActual);
                accSelectedYear.PercentageChangeIncomeActual = _helpers.GetPercentageChange(accPreviousYear.IncomeActual, accSelectedYear.IncomeActual);
                accSelectedYear.PercentageChangeBalanceActual = _helpers.GetPercentageChange(accPreviousYear.BalanceActual, accSelectedYear.BalanceActual);
            }
            else if (selectedYear == CurrentYear)
            {
                accSelectedYear.PercentageChangeExpensesBudget = _helpers.GetPercentageChange(accPreviousYear.ExpensesActual, accSelectedYear.ExpensesBudget);
                accSelectedYear.PercentageChangeIncomeBudget = _helpers.GetPercentageChange(accPreviousYear.IncomeActual, accSelectedYear.IncomeBudget);
                accSelectedYear.PercentageChangeBalanceBudget = _helpers.GetPercentageChange(accPreviousYear.BalanceActual, accSelectedYear.BalanceBudget);

            }
            else if (selectedYear > CurrentYear)
            {
                accSelectedYear.PercentageChangeExpensesBudget = _helpers.GetPercentageChange(accPreviousYear.ExpensesBudget, accSelectedYear.ExpensesBudget);
                accSelectedYear.PercentageChangeIncomeBudget = _helpers.GetPercentageChange(accPreviousYear.IncomeBudget, accSelectedYear.IncomeBudget);
                accSelectedYear.PercentageChangeBalanceBudget = _helpers.GetPercentageChange(accPreviousYear.BalanceBudget, accSelectedYear.BalanceBudget);
            }
        }


        private List<YearTotalsViewModel> GetTotalsForYears(List<int> years, List<AccountYearViewModel> allAccounts)
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

                    if (years[i] < CurrentYear)
                    {
                        ts.PercentageChangeExpensesActualTotal = _helpers.GetPercentageChange(totalsOfPY.ExpensesActualTotal, ts.ExpensesActualTotal);
                        ts.PercentageChangeIncomeActualTotal = _helpers.GetPercentageChange(totalsOfPY.IncomeActualTotal, ts.IncomeActualTotal);
                        ts.PercentageChangeBalanceActualTotal = _helpers.GetPercentageChange(totalsOfPY.BalanceActualTotal, ts.BalanceActualTotal);
                    }
                    else if (years[i] == CurrentYear)
                    {
                        ts.PercentageChangeExpensesBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.ExpensesActualTotal, ts.ExpensesBudgetTotal);
                        ts.PercentageChangeIncomeBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.IncomeActualTotal, ts.IncomeBudgetTotal);
                        ts.PercentageChangeBalanceBudgetTotal = _helpers.GetPercentageChange(totalsOfPY.BalanceActualTotal, ts.BalanceBudgetTotal);
                    }
                    else if (years[i] > CurrentYear)
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
