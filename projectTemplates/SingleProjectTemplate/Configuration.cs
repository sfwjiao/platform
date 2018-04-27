using System.Data.Entity.Migrations;
using Abp.MultiTenancy;
using Abp.Zero.EntityFramework;
using EntityFramework.DynamicFilters;

namespace $projectname$.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<EntityFramework.$projectname$DbContext>, IMultiTenantSeed
    {
        public AbpTenantBase Tenant { get; set; }

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(EntityFramework.$projectname$DbContext context)
        {
            context.DisableAllFilters();

            if (Tenant == null)
            {
            }
            else
            {
            }

            context.SaveChanges();
        }
    }
}
