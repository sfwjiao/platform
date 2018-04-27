using System.Reflection;
using System.Web.Compilation;
using System.Web.Http;
using System.Web.Optimization;
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
using $projectname$.Api;

namespace $projectname$
{
    [DependsOn(
        typeof(AbpZeroCoreModule), 
        typeof(AbpZeroEntityFrameworkModule), 
        typeof(AbpAutoMapperModule),
        typeof(AbpWebApiModule),
        typeof(AbpWebMvcModule)
        )]
    public class $projectname$Module : AbpModule
    {
        public override void PreInitialize()
        {
            //Add/remove localization sources here
            Configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource(
                    $projectname$Consts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        Assembly.GetExecutingAssembly(),
                        "$projectname$.Localization.Source"
                        )
                    )
                );


            Configuration.Modules.AbpAutoMapper().Configurators.Add(mapper =>
            {
            });

            //Configure navigation/menu
            Configuration.Navigation.Providers.Add<$projectname$NavigationProvider>();
            Configuration.Settings.Providers.Add<$projectname$SettingProvider>();

            Configuration.EmbeddedResources.Sources.Add(new EmbeddedResourceSet(
                "/Plugins/$projectname$/",
                Assembly.GetExecutingAssembly(),
                "$projectname$.UI"));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder
                .ForAll<IApplicationService>(typeof($projectname$Module).Assembly, "pt$projectname$")
                .Build();

            Configuration.Modules.AbpWebApi().HttpConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));

            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
