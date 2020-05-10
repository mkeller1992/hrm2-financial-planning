using System.Collections.Generic;

namespace Projekt2.ViewModels
{
    public class AccountMultipleYearsViewModel
    {
        public string Type { get; set; }
        public string AccountId { get; set; }
        public int AccountLevel { get; set; }
        public string AccountName { get; set; }
        public string ParentId { get; set; }
        public string IdOfParentInSuperordinateStructure { get; set; } // refers to id of top-level account, in case subject-/function-groups are mixed

        public List<int> SelectedYears { get; set; }

        public List<AccountYearViewModel> YearlyAccounts { get; set; }

        public List<AccountMultipleYearsViewModel> Children { get; set; }

    }
}
