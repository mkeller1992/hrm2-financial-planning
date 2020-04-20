using Microsoft.EntityFrameworkCore;
using Projekt2.DbModels;
using Projekt2.Services.Enums;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Services
{

    public class DataServiceER
    {
        Project2Context _context;

        public readonly int CURRENT_YEAR = 2017;

        public DataServiceER(Project2Context context)
        {
            _context = context;
        }

        public List<int> GetRelevantYears(int currentYear)
        {
            var result = new List<int>();
            int count = -2;
            while (count < 5)
            {
                result.Add(currentYear + count);
                count++;
            }
            return result;
        }

        public List<int> GetAvailableLevels()
        {
            return new List<int>() { 1, 2, 3, 4 };
        }


        public async Task<YearViewModel> FetchERAccountGroupsForYear(StructureType selectedType, int selectedYear, int selectedLevel, bool allExistingAccounts)
        {
            var query = GetQueryToFetchAccountGroupsER(selectedType, new List<int> { selectedYear }, selectedLevel, allExistingAccounts);
            var accounts = await query.ToListAsync();

            var m = new AccountYearTotalsViewModel
            {
                Year = selectedYear,
                ExpensesActualTotal = accounts.Select(a => a.ExpensesActual).Sum(),
                ExpensesBudgetTotal = accounts.Select(a => a.ExpensesBudget).Sum(),
                IncomeActualTotal = accounts.Select(a => a.IncomeActual).Sum(),
                IncomeBudgetTotal = accounts.Select(a => a.IncomeBudget).Sum(),
            };
            m.BalanceActualTotal = (m.IncomeActualTotal ?? 0) - (m.ExpensesActualTotal ?? 0);
            m.BalanceBudgetTotal = (m.IncomeBudgetTotal ?? 0) - (m.ExpensesBudgetTotal ?? 0);

            return new YearViewModel
            {
                Year = selectedYear,
                Accounts = accounts.ToList(),
                AccountYearTotals = m
            };
        }


        public async Task<List<AccountYearViewModel>> FetchSubGroupsOfERAccountStatic(AccountYearViewModel ayear, StructureType selectedType, bool allExistingAccounts)
        {
            if (ayear.AccountLevel >= 4)
            {
                return null;
            }
            YearViewModel m = await FetchERAccountGroupsForYear(selectedType, ayear.Year, (ayear.AccountLevel) + 1, allExistingAccounts);

            return m.Accounts.Where(a => a.AccountId.Substring(0, ayear.AccountLevel) == ayear.AccountId).ToList();
        }


        public async Task<MultipleYearsViewModel> FetchAccountGroupsForTimeline(StructureType selectedType, ERAccountType erAccountType, List<int> selectedYears, int selectedLevel, bool allExistingAccounts)
        {
            var query = GetQueryToFetchAccountGroupsER(selectedType, selectedYears, selectedLevel, allExistingAccounts);
            List<AccountYearViewModel> accounts = await query.ToListAsync();

            List<string> accountIds = new List<string>();

            if (selectedType == StructureType.Functions)
            {
                accountIds = accounts.Select(a => a.AccountId)
                                     .OrderBy(a => a)
                                     .Distinct()
                                     .ToList();
            }
            // in case of "Gliederung nach Sachgruppen" map only accounts that correspond to selected type of ER-account (=> Aufwand or Ertrag)
            else if (selectedType == StructureType.Subjects)
            {
                // all "Aufwand"-accounts resp. "Ertrag"-accounts start with the same number => use this fact for filtering:
                string firstDigit = erAccountType == ERAccountType.Expenses ? "3" : "4";
                accountIds = accounts.Where(a => a.AccountId.Substring(0, 1) == firstDigit)
                                     .Select(a => a.AccountId)
                                     .OrderBy(a => a)
                                     .Distinct()
                                     .ToList();
            }

            List<AccountYearTotalsViewModel> totalsForSelectedYears = new List<AccountYearTotalsViewModel>();

            foreach (var year in selectedYears)
            {
                var m = new AccountYearTotalsViewModel
                {
                    Year = year,
                    ExpensesActualTotal = accounts.Where(a => a.Year == year).Select(a => a.ExpensesActual).Sum(),
                    ExpensesBudgetTotal = accounts.Where(a => a.Year == year).Select(a => a.ExpensesBudget).Sum(),
                    IncomeActualTotal = accounts.Where(a => a.Year == year).Select(a => a.IncomeActual).Sum(),
                    IncomeBudgetTotal = accounts.Where(a => a.Year == year).Select(a => a.IncomeBudget).Sum(),
                };
                m.BalanceActualTotal = (m.IncomeActualTotal ?? 0) - (m.ExpensesActualTotal ?? 0);
                m.BalanceBudgetTotal = (m.IncomeBudgetTotal ?? 0) - (m.ExpensesBudgetTotal ?? 0);
                totalsForSelectedYears.Add(m);
            }

            var result = new MultipleYearsViewModel
            {
                SelectedYears = selectedYears,
                ListOfAccountYearTotals = totalsForSelectedYears,
                AccountsWithMultipleYears = new List<AccountMultipleYearsViewModel>()
            };

            foreach (var id in accountIds)
            {
                var yearlyAccounts = new List<AccountYearViewModel>();
                foreach (var year in selectedYears)
                {
                    var yearlyAccount = accounts.Where(a => a.AccountId == id && a.Year == year).ToList();

                    if (yearlyAccount.Count == 0)
                    {
                        var ya = accounts.Where(a => a.AccountId == id).ToList();
                        if (ya.Count == 1)
                        {
                            ya[0].Year = year;
                            yearlyAccounts.Add(ya[0]);
                        }
                    }
                    else if (yearlyAccount.Count == 1)
                    {
                        yearlyAccounts.Add(yearlyAccount[0]);
                    }
                    else
                    {
                        throw new Exception("Too many matching accounts!");
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


        public async Task<List<AccountMultipleYearsViewModel>> FetchSubGroupsOfERAccountTimeline(AccountMultipleYearsViewModel accMultiYears, StructureType selectedType, ERAccountType selectedERAccountType, bool allExistingAccounts)
        {
            if (accMultiYears.AccountLevel >= 4)
            {
                return null;
            }
            MultipleYearsViewModel m = await FetchAccountGroupsForTimeline(selectedType, selectedERAccountType, accMultiYears.SelectedYears, (accMultiYears.AccountLevel + 1), allExistingAccounts);

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
                                                Id = o.Key.FunctionId != null ? o.Key.FunctionId : o.Key.SubjectId, // corresponds to property of groupBy condition
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


    }
}
