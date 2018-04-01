using System.Data.Entity;
using System.Reflection;
using Abp.Modules;
using Abp.Zero.EntityFramework;
using Platform.EntityFramework;

namespace Platform
{
    [DependsOn(typeof(AbpZeroEntityFrameworkModule), typeof(PlatformCoreModule))]
    public class PlatformDataModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.DefaultNameOrConnectionString = "Default";
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
            Database.SetInitializer<PlatformDbContext>(null);
        }
    }
}
