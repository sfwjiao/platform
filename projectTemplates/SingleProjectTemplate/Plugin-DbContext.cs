using System.Data.Common;
using System.Data.Entity;
using Abp.EntityFramework;

namespace $projectname$.EntityFramework
{
    public class $projectname$DbContext  : AbpDbContext
    {
        public $projectname$DbContext()
            : base("Default")
        {

        }

        public $projectname$DbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }
        
        public $projectname$DbContext(DbConnection existingConnection)
         : base(existingConnection, false)
        {

        }

        public $projectname$DbContext(DbConnection existingConnection, bool contextOwnsConnection)
         : base(existingConnection, contextOwnsConnection)
        {

        }
    }
}
