using System.Collections.Generic;
using Abp.Configuration;
using Abp.Localization;

namespace Cms.Api
{
    public class CmsSettingProvider : SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                //
                CreateNavigationSetting(NavigationNames.Cms, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.Cms, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.Cms, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.Cms, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.Cms, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.Cms, NavigationNames.Propertys.Url, null),
                
                //
                CreateNavigationSetting(NavigationNames.CmsManage, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.CmsManage, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.CmsManage, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.CmsManage, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.CmsManage, NavigationNames.Propertys.Icon, "fa fa-key"),
                CreateNavigationSetting(NavigationNames.CmsManage, NavigationNames.Propertys.Url, "#/Cms_manage"),
            };
        }

        private static SettingDefinition CreateNavigationSetting(string nav, string property, string value)
        {
            return new SettingDefinition(
                NavigationNames.GetName(nav, property),
                value,
                L($"{nav}{property}"),
                scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                isVisibleToClients: true);
        }

        private static LocalizableString L(string name)
        {
            return new LocalizableString(name, CmsConsts.LocalizationSourceName);
        }
    }
}
