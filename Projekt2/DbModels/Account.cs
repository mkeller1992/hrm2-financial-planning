using System;
using System.Collections.Generic;

namespace Projekt2.DbModels
{
    public partial class Account
    {
        public Account()
        {
            AccountYear = new HashSet<AccountYear>();
        }

        public string Type { get; set; }
        public string SubjectId { get; set; }
        public string FunctionType { get; set; }
        public string FunctionId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }

        public virtual AccountGroup AccountGroup { get; set; }
        public virtual AccountGroup Function { get; set; }
        public virtual ICollection<AccountYear> AccountYear { get; set; }
    }
}
