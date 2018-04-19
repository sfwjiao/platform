using System.Linq;
using Platform.EntityFramework;
using Platform.MultiTenancy;

namespace Platform.Migrations.SeedData
{
    public class DefaultTenantCreator
    {
        private readonly PlatformDbContext _context;

        public DefaultTenantCreator(PlatformDbContext context)
        {
            _context = context;
        }

        public int Create()
        {
            return CreateUserAndRoles();
        }

        private int CreateUserAndRoles()
        {
            //Default tenant

            var defaultTenant = _context.Tenants.FirstOrDefault(t => t.TenancyName == Tenant.DefaultTenantName);
            if (defaultTenant == null)
            {
                _context.Tenants.Add(new Tenant {TenancyName = Tenant.DefaultTenantName, Name = Tenant.DefaultTenantName});
                _context.SaveChanges();
                defaultTenant = _context.Tenants.FirstOrDefault(t => t.TenancyName == Tenant.DefaultTenantName);
            }

            return defaultTenant.Id;
        }
    }
}
