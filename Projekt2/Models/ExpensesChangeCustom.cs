using Projekt2.DbModels;
using Projekt2.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Models
{
    public class ExpensesChangeCustom : IModificationOperation
    {
        private readonly List<int> _increments;

        public ExpensesChangeCustom(List<int> increments)
        {
            if (increments.Count != 5) 
            {
                throw new Exception("Invalid number of increments specified!");
            }
            _increments = increments;
        }

        public void ApplyModification(int financialYear, AccountYear accountPreviousYear, AccountYear accountSelectedYear)
        {
           
            // TODO: Implement !!!


        }
    }
}
