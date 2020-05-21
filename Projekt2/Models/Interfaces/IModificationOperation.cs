using Projekt2.DbModels;
using Projekt2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models.Interfaces
{
    public interface IModificationOperation
    {
        public void ApplyModification(int baseYear, AccountYearDto accountPreviousYear, AccountYearDto accountSelectedYear);

    }
}
