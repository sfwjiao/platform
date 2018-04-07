using System.Data.Common;
using System.Data.Entity;
using Abp.Zero.EntityFramework;
using Platform.Authorization.Roles;
using Platform.MultiTenancy;
using Platform.Users;

namespace Platform.EntityFramework
{
    public class PlatformDbContext : AbpZeroDbContext<Tenant, Role, User>
    {
        //TODO: Define an IDbSet for your Entities...
        public virtual IDbSet<AuditLogInclude> AuditLogIncludes { get; set; }

        /* NOTE: 
         *   Setting "Default" to base class helps us when working migration commands on Package Manager Console.
         *   But it may cause problems when working Migrate.exe of EF. If you will apply migrations on command line, do not
         *   pass connection string name to base classes. ABP works either way.
         */
        public PlatformDbContext()
            : base("Default")
        {

        }

        /* NOTE:
         *   This constructor is used by ABP to pass connection string defined in PlatformDataModule.PreInitialize.
         *   Notice that, actually you will not directly create an instance of PlatformDbContext since ABP automatically handles it.
         */
        public PlatformDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }

        //This constructor is used in tests
        public PlatformDbContext(DbConnection existingConnection)
         : base(existingConnection, false)
        {

        }

        public PlatformDbContext(DbConnection existingConnection, bool contextOwnsConnection)
         : base(existingConnection, contextOwnsConnection)
        {

        }
    }
}
