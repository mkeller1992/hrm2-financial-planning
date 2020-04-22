using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Services
{
    public class HelpersService
    {
        public string FormatDecimal(decimal? number) {

            if (number == null) 
            {
                return "0";
            }
            return string.Format("{0:n0}", number);
        }

        public bool IsPositive(decimal? number) {
            return number == null || number >= 0;        
        }

    }
}
