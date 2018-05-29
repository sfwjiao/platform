using System.Reflection;
using System.Web.Compilation;
using System.Web.Http;
using System.Web.Optimization;
using System.Web.Routing;
using Abp.Application.Services;
using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.Resources.Embedded;
using Abp.Web.Mvc;
using Abp.WebApi;
using Abp.Zero;
using Abp.Zero.EntityFramework;
using Cms.Api;

namespace Cms
{
    [DependsOn(
        typeof(AbpZeroCoreModule),
        typeof(AbpZeroEntityFrameworkModule),
        typeof(AbpAutoMapperModule),
        typeof(AbpWebApiModule),
        typeof(AbpWebMvcModule)
        )]
    public class CmsModule : AbpModule
    {
        public override void PreInitialize()
        {
            //Add/remove localization sources here
            Configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    CmsConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        Assembly.GetExecutingAssembly(),
                        "Cms.Localization.Source"
                        )
                    )
                );


            Configuration.Modules.AbpAutoMapper().Configurators.Add(mapper =>
            {
            });

            //Configure navigation/menu
            Configuration.Navigation.Providers.Add<CmsNavigationProvider>();
            Configuration.Settings.Providers.Add<CmsSettingProvider>();

            Configuration.EmbeddedResources.Sources.Add(new EmbeddedResourceSet(
                "/Plugins/Cms/",
                Assembly.GetExecutingAssembly(),
                "Cms.UI"));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
                .ForAll<IApplicationService>(typeof(CmsModule).Assembly, "pCms")
                .Build();

            Configuration.Modules.AbpWebApi().HttpConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));
            
            RouteConfig.RegisterRoutes(RouteTable.Routes, IocManager);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
