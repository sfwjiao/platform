using System.Collections.Generic;
using Abp.Configuration;
using Abp.Localization;

namespace $projectname$.Api
{
    public class $projectname$SettingProvider: SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                //
                CreateNavigationSetting(NavigationNames.$projectname$, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.$projectname$, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.$projectname$, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.$projectname$, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.$projectname$, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.$projectname$, NavigationNames.Propertys.Url, null),
                
                //
                CreateNavigationSetting(NavigationNames.$projectname$Manage, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.$projectname$Manage, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.$projectname$Manage, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.$projectname$Manage, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.$projectname$Manage, NavigationNames.Propertys.Icon, "fa fa-key"),
                CreateNavigationSetting(NavigationNames.$projectname$Manage, NavigationNames.Propertys.Url, "#/$projectname$_manage"),
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
            return new LocalizableString(name, $projectname$Consts.LocalizationSourceName);
        }
    }
}
