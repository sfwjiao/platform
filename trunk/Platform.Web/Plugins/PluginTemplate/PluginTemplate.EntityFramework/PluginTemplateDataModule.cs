using System.Reflection;
using Abp.Modules;
using Abp.Zero.EntityFramework;

namespace PluginTemplate
{
    [DependsOn(typeof(AbpZeroEntityFrameworkModule), typeof(PluginTemplateCoreModule))]
    public class PluginTemplateDataModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }
    }
}
