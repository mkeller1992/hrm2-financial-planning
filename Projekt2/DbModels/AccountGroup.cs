using System;
using System.Collections.Generic;

namespace Projekt2.DbModels
{
    public partial class AccountGroup
    {
        public AccountGroup()
        {
            AccountAccountGroup = new HashSet<Account>();
            AccountFunction = new HashSet<Account>();
            InverseAccountGroupNavigation = new HashSet<AccountGroup>();
        }

        public string Type { get; set; }
        public string Id { get; set; }
        public string ParentId { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual AccountGroup AccountGroupNavigation { get; set; }
        public virtual ICollection<Account> AccountAccountGroup { get; set; }
        public virtual ICollection<Account> AccountFunction { get; set; }
        public virtual ICollection<AccountGroup> InverseAccountGroupNavigation { get; set; }
    }
}
