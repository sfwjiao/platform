using System.Data.Entity.Migrations;
using Abp.MultiTenancy;
using Abp.Zero.EntityFramework;
using EntityFramework.DynamicFilters;

namespace SingleProjectTemplate.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<EntityFramework.SingleProjectTemplateDbContext>, IMultiTenantSeed
    {
        public AbpTenantBase Tenant { get; set; }

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(EntityFramework.SingleProjectTemplateDbContext context)
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
