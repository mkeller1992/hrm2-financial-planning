using Microsoft.EntityFrameworkCore;
using Projekt2.DbModels;
using Projekt2.Services.Enums;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Internal;

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
            var years = new List<int> {previousYear, selectedYear};

            var query = GetQueryToFetchAccountGroupsER(selectedType, years, selectedLevel, allExistingAccounts);

            List<AccountYearViewModel> allAccounts = await query.ToListAsync();
            List<AccountYearViewModel> accountsForPreviousYear = allAccounts.Where(a => a.Year == previousYear).ToList();
            List<AccountYearViewModel> accountsForSelectedYear = allAccounts.Where(a => a.Year == selectedYear).ToList();

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
            YearViewModel m = await FetchMainGroupsForYearlyER(selectedType, ayear.Year, (ayear.AccountLevel) + 1, allExistingAccounts);

            return m.Accounts.Where(a => a.AccountId.Substring(0, ayear.AccountLevel) == ayear.AccountId).ToList();
        }


        /** Timeline ER - Fetch main-groups **/
        public async Task<MultipleYearsViewModel> FetchMainGroupsForTimelineEr(StructureType selectedType, ERAccountType erAccountType, List<int> selectedYears, int selectedLevel, bool allExistingAccounts)
        {
            var query = GetQueryToFetchAccountGroupsER(selectedType, selectedYears, selectedLevel, allExistingAccounts);

            // accounts contains all accounts as a flat list:
            List<AccountYearViewModel> allAccounts = await query.ToListAsync();

            List<string> accountIds = new List<string>();

            if (selectedType == StructureType.Functions)
            {
                accountIds = allAccounts.Select(a => a.AccountId)
                                     .OrderBy(a => a)
                                     .Distinct()
                                     .ToList();
            }
            // in case of "Gliederung nach Sachgruppen" map only accounts that correspond to selected type of ER-account (=> Aufwand or Ertrag)
            else if (selectedType == StructureType.Subjects)
            {
                // all "Aufwand"-accounts resp. "Ertrag"-accounts start with the same number => use this fact for filtering:
                string firstDigit = erAccountType == ERAccountType.Expenses ? "3" : "4";
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


        /** Timeline ER - Fetch SUB-groups **/
        public async Task<List<AccountMultipleYearsViewModel>> FetchSubGroupsForTimelineEr(AccountMultipleYearsViewModel accMultiYears, StructureType selectedType, ERAccountType selectedERAccountType, bool allExistingAccounts)
        {
            if (accMultiYears.AccountLevel >= 4)
            {
                return null;
            }
            MultipleYearsViewModel m = await FetchMainGroupsForTimelineEr(selectedType, selectedERAccountType, accMultiYears.SelectedYears, (accMultiYears.AccountLevel + 1), allExistingAccounts);

            return m.AccountsWithMultipleYears.Where(a => a.AccountId.Substring(0, accMultiYears.AccountLevel) == accMultiYears.AccountId).ToList();
        }


        public IQueryable<AccountYearViewModel> GetQueryToFetchAccountGroupsER(StructureType selectedType, List<int> selectedYears, int selectedLevel, bool allExistingAccounts)
        {

            var accountYears = _context.AccountYear.Where(entry => selectedYears.Contains(entry.Year) && entry.Type == "ER")
                                            .GroupBy(ayear => new
                                            {
                                                FunctionId = selectedType == StructureType.Functions ? ayear.FunctionId.Substring(0, selectedLevel) : null,
                                                SubjectId = selectedType == StructureType.Subjects ? ayear.SubjectId.Substring(0, selectedLevel) : null,
                                                ayear.Year
                                            })
                                            .Select(o => new
                                            {
                                                Id = o.Key.FunctionId ?? o.Key.SubjectId, // corresponds to property of groupBy condition
                                                o.Key.Year, // property-name is inferred (=> 'Year')
                                                ExpensesBudget = o.Sum(x => x.ExpensesBudget),
                                                ExpensesActual = o.Sum(x => x.ExpensesEffective),
                                                IncomeBudget = o.Sum(x => x.IncomeBudget),
                                                IncomeActual = o.Sum(x => x.IncomeEffective)
                                            });

            var relevantType = selectedType == StructureType.Functions ? "FG" : "ER";

            IQueryable<AccountGroup> accountGroups = _context.AccountGroup.Where(a => a.Type == relevantType &&
                                                                                     a.Level == selectedLevel);

            // In case user wants to see all accounts, including the unused:
            if (allExistingAccounts)
            {
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


        private void SetPercentChangesBetweenTwoYears(AccountYearViewModel accPreviousYear, AccountYearViewModel accSelectedYear, int selectedYear)
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
                    else if(years[i] > CurrentYear)
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
