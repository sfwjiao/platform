using System.Reflection;
using System.Web.Http;
using System.Web.Optimization;
using Abp.Application.Services;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Resources.Embedded;
using Abp.Web.Mvc.Resources;
using Abp.WebApi;

namespace Examination.Api
{
    [DependsOn(typeof(AbpWebApiModule), typeof(ExaminationApplicationModule))]
    public class ExaminationWebApiModule : AbpModule
    {

        public override void PreInitialize()
        {
            //Configure navigation/menu
            Configuration.Navigation.Providers.Add<ExaminationNavigationProvider>();
            Configuration.Settings.Providers.Add<ExaminationSettingProvider>();

            Configuration.EmbeddedResources.Sources.Add(new EmbeddedResourceSet(
                "/App/Main/",
                Assembly.GetExecutingAssembly(),
                "Examination.UI"));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
                .ForAll<IApplicationService>(typeof(ExaminationApplicationModule).Assembly, "exam")
                .Build();

            Configuration.Modules.AbpWebApi().HttpConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));

            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
