using System.Reflection;
using Abp.AutoMapper;
using Abp.Modules;

namespace Examination
{
    [DependsOn(typeof(ExaminationCoreModule), typeof(AbpAutoMapperModule))]
    public class ExaminationApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Modules.AbpAutoMapper().Configurators.Add(mapper =>
            {
            });
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}
