using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Constants
{
    public class Const
    {

        public static readonly int CurrentYear = 2017;

        public static readonly int NumberOfRelevantPastYears = 2;

        public static readonly int NumberOfRelevantBudgetYears = 5;

        public static readonly int OldestFinanceYear = 2015;

        public static readonly int NewestFinancialYear = 2018;

        public static readonly string FirstDigitOfExpenses = "3";
        public static readonly string FirstDigitOfIncomes = "4";

        public static readonly int MaxLengthOfSubjectAccount = 4;
        public static readonly int MaxLengthOfFunctionAccount = 4;

        public static readonly string DescrPercentIncomeIncrease = "% / Jahr => Ertragssteigerung";
        public static readonly string DescrPercentIncomeDecrease = "% / Jahr => Ertragsrückgang";

        public static readonly string DescrPercentExpensesIncrease = "% / Jahr => Aufwandsteigerung";
        public static readonly string DescrPercentExpensesDecrease = "% / Jahr => Aufwandsenkung";
    }
}
