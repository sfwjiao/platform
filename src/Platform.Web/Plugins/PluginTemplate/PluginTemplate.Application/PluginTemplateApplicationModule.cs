using System.Reflection;
using Abp.AutoMapper;
using Abp.Modules;

namespace PluginTemplate
{
    [DependsOn(typeof(PluginTemplateCoreModule), typeof(AbpAutoMapperModule))]
    public class PluginTemplateApplicationModule : AbpModule
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
