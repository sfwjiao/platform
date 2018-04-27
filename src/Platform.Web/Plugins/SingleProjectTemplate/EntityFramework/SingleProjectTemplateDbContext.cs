using System.Data.Common;
using System.Data.Entity;
using Abp.EntityFramework;
using SingleProjectTemplate.Core;

namespace SingleProjectTemplate.EntityFramework
{
    public class SingleProjectTemplateDbContext  : AbpDbContext
    {
        public virtual IDbSet<Customer> Customers { get; set; }

        public SingleProjectTemplateDbContext()
            : base("Default")
        {

        }

        public SingleProjectTemplateDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }
        
        public SingleProjectTemplateDbContext(DbConnection existingConnection)
         : base(existingConnection, false)
        {

        }

        public SingleProjectTemplateDbContext(DbConnection existingConnection, bool contextOwnsConnection)
         : base(existingConnection, contextOwnsConnection)
        {

        }
    }
}
