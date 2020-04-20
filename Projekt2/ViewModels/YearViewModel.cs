using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.ViewModels
{
    public class YearViewModel
    {
        public int Year { get; set; }
        public AccountYearTotalsViewModel AccountYearTotals { get; set; }
        public List<AccountYearViewModel> Accounts { get; set; }
    }
}
