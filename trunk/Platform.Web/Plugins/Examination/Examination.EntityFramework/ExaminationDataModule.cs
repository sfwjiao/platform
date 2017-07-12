using System.Reflection;
using Abp.Modules;
using Abp.Zero.EntityFramework;

namespace Examination
{
    [DependsOn(typeof(AbpZeroEntityFrameworkModule), typeof(ExaminationCoreModule))]
    public class ExaminationDataModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}
