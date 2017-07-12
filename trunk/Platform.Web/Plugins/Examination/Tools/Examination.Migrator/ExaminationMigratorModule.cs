using System.Data.Entity;
using System.Reflection;
using Abp.Modules;
using Examination.EntityFramework;

namespace Examination.Migrator
{
    [DependsOn(typeof(ExaminationDataModule))]
    public class ExaminationMigratorModule : AbpModule
    {
        public override void PreInitialize()
        {
            Database.SetInitializer<ExaminationDbContext>(null);

            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}