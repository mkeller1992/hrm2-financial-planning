using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projekt2.Services.Enums
{
    public class DropdownLabels
    {

        public static string GetLabelFor(AccountRange a)
        {
            return a switch
            {
                AccountRange.UsedAccounts => "Tatsächlich verwendete Konten",
                AccountRange.AllAccounts => "Alle existierenden Konten",
                AccountRange.None => "Umfang",
                _ => ""
            };
        }

        public static string GetLabelFor(StructureType t)
        {
            return t switch
            {
                StructureType.Functions => "Nach Funktionsgruppen",
                StructureType.Subjects => "Nach Sachgruppen",
                StructureType.None => "Gliederungsart",
                _ => ""
            };
        }

        public static string GetLabelFor(ERAccountType t)
        {
            return t switch
            {
                ERAccountType.Expenses => "Aufwände",
                ERAccountType.Income => "Erträge",
                ERAccountType.Balances => "Saldi",
                ERAccountType.None => "ER-Kontentyp",
                _ => ""
            };
        }


    }
}
