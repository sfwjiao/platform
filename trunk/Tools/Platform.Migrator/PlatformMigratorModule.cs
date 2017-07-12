using System.Data.Entity;
using System.Reflection;
using Abp.Modules;
using Platform.EntityFramework;

namespace Platform.Migrator
{
    [DependsOn(typeof(PlatformDataModule))]
    public class PlatformMigratorModule : AbpModule
    {
        public override void PreInitialize()
        {
            Database.SetInitializer<PlatformDbContext>(null);

            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}