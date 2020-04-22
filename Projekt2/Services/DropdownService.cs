using Projekt2.Services.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Services
{
    public class DropdownService
    {
        public Dictionary<int, string> Years(List<int> relevantYears)
        {
            var result = new Dictionary<int, string>();
            foreach(int y in relevantYears)
            {
                result.Add(y, $"{y}");
            }
            return result;
        }

        public readonly Dictionary<int, string> Levels = new Dictionary<int, string>()
                {
                    {1, "1"},
                    {2, "2"},
                    {3, "3"},
                    {4, "4"}
                };

        public readonly Dictionary<string, string> AccountRanges = new Dictionary<string, string>()
                {
                    {AccountRange.UsedAccounts.ToString(), "Tatsächlich verwendete Konten"},
                    {AccountRange.AllAccounts.ToString(), "Alle existierenden Konten"},
                };


        public readonly Dictionary<string, string> StructureTypes = new Dictionary<string, string>()
                {
                    {StructureType.Functions.ToString(), "Nach Funktionsgruppen"},
                    {StructureType.Subjects.ToString(), "Nach Sachgruppen"},
                };


        public readonly Dictionary<string, string> ERAccountTypes = new Dictionary<string, string>()
                {
                    {ERAccountType.Expenses.ToString(), "Aufwände"},
                    {ERAccountType.Income.ToString(), "Erträge"},
                    {ERAccountType.Balances.ToString(), "Saldi"},
                };

    }
}
