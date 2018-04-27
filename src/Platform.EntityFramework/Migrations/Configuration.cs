using System.Data.Entity.Migrations;
using Abp.MultiTenancy;
using Abp.Zero.EntityFramework;
using Platform.Migrations.SeedData;
using EntityFramework.DynamicFilters;

namespace Platform.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<Platform.EntityFramework.PlatformDbContext>, IMultiTenantSeed
    {
        public AbpTenantBase Tenant { get; set; }

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Platform";
        }

        protected override void Seed(Platform.EntityFramework.PlatformDbContext context)
        {
            context.DisableAllFilters();

            if (Tenant == null)
            {
                //Host seed
                new InitialHostDbBuilder(context).Create();

                //Default tenant seed (in host database).
                var defaultTenantId = new DefaultTenantCreator(context).Create();
                new TenantRoleAndUserBuilder(context, defaultTenantId).Create();
            }
            else
            {
                //You can add seed for tenant databases and use Tenant property...
            }

            context.SaveChanges();
        }
    }
}
