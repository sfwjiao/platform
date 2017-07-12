using System.Data.Entity.Migrations;
using Abp.MultiTenancy;
using Abp.Zero.EntityFramework;
using EntityFramework.DynamicFilters;

namespace PluginTemplate.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<EntityFramework.PluginTemplateDbContext>, IMultiTenantSeed
    {
        public AbpTenantBase Tenant { get; set; }

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            //MigrationsDirectory = @"Migrations";
        }

        protected override void Seed(EntityFramework.PluginTemplateDbContext context)
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
