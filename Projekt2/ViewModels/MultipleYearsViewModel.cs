using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.ViewModels
{
    public class MultipleYearsViewModel
    {
        public List<int> SelectedYears { get; set; }
        public List<AccountYearTotalsViewModel> ListOfAccountYearTotals { get; set; }
        public List<AccountMultipleYearsViewModel> AccountsWithMultipleYears { get; set; }
    }
}
