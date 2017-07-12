using System.Reflection;
using System.Web.Http;
using System.Web.Optimization;
using Abp.Application.Services;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Resources.Embedded;
using Abp.Web.Mvc.Resources;
using Abp.WebApi;

namespace PluginTemplate.Api
{
    [DependsOn(typeof(AbpWebApiModule), typeof(PluginTemplateApplicationModule))]
    public class PluginTemplateWebApiModule : AbpModule
    {

        public override void PreInitialize()
        {
            //Configure navigation/menu
            Configuration.Navigation.Providers.Add<PluginTemplateNavigationProvider>();
            Configuration.Settings.Providers.Add<PluginTemplateSettingProvider>();

            Configuration.EmbeddedResources.Sources.Add(new EmbeddedResourceSet(
                "/App/Main/",
                Assembly.GetExecutingAssembly(),
                "PluginTemplate.UI"));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
                .ForAll<IApplicationService>(typeof(PluginTemplateApplicationModule).Assembly, "ptemplate")
                .Build();

            Configuration.Modules.AbpWebApi().HttpConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));

            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
