using System.Data.Entity.Migrations;
using Abp.MultiTenancy;
using Abp.Zero.EntityFramework;
using EntityFramework.DynamicFilters;

namespace Cms.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<EntityFramework.CmsDbContext>, IMultiTenantSeed
    {
        public AbpTenantBase Tenant { get; set; }

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(EntityFramework.CmsDbContext context)
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
