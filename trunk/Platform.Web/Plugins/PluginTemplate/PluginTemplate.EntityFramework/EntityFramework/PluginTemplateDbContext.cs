using System.Data.Common;
using System.Data.Entity;
using Abp.EntityFramework;
using PluginTemplate.Custom;

namespace PluginTemplate.EntityFramework
{
    public class PluginTemplateDbContext  : AbpDbContext
    {
        public virtual IDbSet<Customer> Customers { get; set; }

        public PluginTemplateDbContext()
            : base("Default")
        {

        }

        public PluginTemplateDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }
        
        public PluginTemplateDbContext(DbConnection existingConnection)
         : base(existingConnection, false)
        {

        }

        public PluginTemplateDbContext(DbConnection existingConnection, bool contextOwnsConnection)
         : base(existingConnection, contextOwnsConnection)
        {

        }
    }
}
