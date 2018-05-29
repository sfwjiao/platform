using System.Data.Common;
using System.Data.Entity;
using Abp.EntityFramework;

namespace Cms.EntityFramework
{
    public class CmsDbContext : AbpDbContext
    {
        public CmsDbContext()
            : base("Default")
        {

        }

        public CmsDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }

        public CmsDbContext(DbConnection existingConnection)
         : base(existingConnection, false)
        {

        }

        public CmsDbContext(DbConnection existingConnection, bool contextOwnsConnection)
         : base(existingConnection, contextOwnsConnection)
        {

        }
    }
}
