using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.ViewModels
{
    public class AccountMultipleYearsViewModel
    {
        public string Type { get; set; }
        public string AccountId { get; set; }
        public int AccountLevel { get; set; }
        public string AccountName { get; set; }
        public string ParentId { get; set; }
        public List<int> SelectedYears { get; set; }

        public List<AccountYearViewModel> YearlyAccounts { get; set; }

        public List<AccountMultipleYearsViewModel> Children { get; set; }

    }
}
