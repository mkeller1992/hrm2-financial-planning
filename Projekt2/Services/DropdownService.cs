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
            foreach (int y in relevantYears)
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

        public readonly Dictionary<string, string> ScenarioAccountTypes = new Dictionary<string, string>()
                {
                    { ERAccountType.Expenses.ToString(), "Alle Aufwände" },
                    { ERAccountType.Income.ToString(), "Alle Erträge" },
                    { ERAccountType.ExpensesAndIncomes.ToString(), "Alle Aufwände und Erträge" }
                };

        public readonly Dictionary<string, string> ScenarioTypes = new Dictionary<string, string>()
                {
                    {ScenarioType.InputData.ToString(), "Input Konti"},
                    {ScenarioType.Scenario.ToString(), "Szenario Konti"},
                };


        public readonly Dictionary<string, string> StructureTypes = new Dictionary<string, string>()
                {
                    {StructureType.Functions.ToString(), "Nach Funktionsgruppen"},
                    {StructureType.Subjects.ToString(), "Nach Sachgruppen"},
                    {StructureType.FunctionsThenSubjects.ToString(), "Nach Funktions-/ Sachgruppen"},
                    {StructureType.SubjectsThenFunctions.ToString(), "Nach Sach-/ Funktionsgruppen"},
                };


        public readonly Dictionary<string, string> ERAccountTypes = new Dictionary<string, string>()
                {
                    {ERAccountType.Expenses.ToString(), "Aufwände"},
                    {ERAccountType.Income.ToString(), "Erträge"},
                    {ERAccountType.Balances.ToString(), "Saldi"},
                };

    }
}
