using System.Data.Entity;
using System.Reflection;
using Abp.Modules;
using PluginTemplate.EntityFramework;

namespace PluginTemplate.Migrator
{
    [DependsOn(typeof(PluginTemplateDataModule))]
    public class PluginTemplateMigratorModule : AbpModule
    {
        public override void PreInitialize()
        {
            Database.SetInitializer<PluginTemplateDbContext>(null);

            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}