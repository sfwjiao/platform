using System.Data.Entity;
using System.Reflection;
using Abp.Modules;
using SingleProjectTemplate;
using SingleProjectTemplate.EntityFramework;

namespace SingleProjectTemplate.Migrator
{
    [DependsOn(typeof(SingleProjectTemplateModule))]
    public class SingleProjectTemplateMigratorModule : AbpModule
    {
        public override void PreInitialize()
        {
            Database.SetInitializer<SingleProjectTemplateDbContext>(null);

            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}