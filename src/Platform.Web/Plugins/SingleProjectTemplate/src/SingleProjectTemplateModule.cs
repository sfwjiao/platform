using System.Reflection;
using System.Web.Http;
using System.Web.Optimization;
using Abp.Application.Services;
using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.Resources.Embedded;
using Abp.WebApi;
using Abp.Zero;
using Abp.Zero.EntityFramework;
using SingleProjectTemplate.Api;

namespace SingleProjectTemplate
{
    [DependsOn(
        typeof(AbpZeroCoreModule), 
        typeof(AbpZeroEntityFrameworkModule), 
        typeof(AbpAutoMapperModule),
        typeof(AbpWebApiModule)
        )]
    public class SingleProjectTemplateModule : AbpModule
    {
        public override void PreInitialize()
        {
            //Add/remove localization sources here
            Configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    SingleProjectTemplateConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        Assembly.GetExecutingAssembly(),
                        "SingleProjectTemplate.Localization.Source"
                        )
                    )
                );


            Configuration.Modules.AbpAutoMapper().Configurators.Add(mapper =>
            {
            });

            //Configure navigation/menu
            Configuration.Navigation.Providers.Add<SingleProjectTemplateNavigationProvider>();
            Configuration.Settings.Providers.Add<SingleProjectTemplateSettingProvider>();

            Configuration.EmbeddedResources.Sources.Add(new EmbeddedResourceSet(
                "/Plugins/SingleProjectTemplate/",
                Assembly.GetExecutingAssembly(),
                "SingleProjectTemplate.UI"));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
                .ForAll<IApplicationService>(typeof(SingleProjectTemplateModule).Assembly, "ptemplate")
                .Build();

            Configuration.Modules.AbpWebApi().HttpConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));

            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
