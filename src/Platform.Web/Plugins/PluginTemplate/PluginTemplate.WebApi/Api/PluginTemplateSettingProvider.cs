using System.Collections.Generic;
using Abp.Configuration;
using Abp.Localization;

namespace PluginTemplate.Api
{
    public class PluginTemplateSettingProvider: SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                //
                CreateNavigationSetting(NavigationNames.Custom, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.Custom, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.Custom, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.Custom, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.Custom, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.Custom, NavigationNames.Propertys.Url, null),

                
                //
                CreateNavigationSetting(NavigationNames.CustomCustomer, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.CustomCustomer, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.CustomCustomer, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.CustomCustomer, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.CustomCustomer, NavigationNames.Propertys.Icon, "fa fa-key"),
                CreateNavigationSetting(NavigationNames.CustomCustomer, NavigationNames.Propertys.Url, "#/custom_customer"),
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
            return new LocalizableString(name, PluginTemplateConsts.LocalizationSourceName);
        }
    }
}
