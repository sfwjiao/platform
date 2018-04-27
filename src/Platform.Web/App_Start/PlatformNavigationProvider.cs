using Abp.Application.Navigation;
using Abp.Localization;
using Platform.Authorization;

namespace Platform.Web
{
    /// <summary>
    /// This class defines menus for the application.
    /// It uses ABP's menu system.
    /// When you add menu items here, they are automatically appear in angular application.
    /// See .cshtml and .js files under App/Main/views/layout/header to know how to render menu.
    /// </summary>
    public class PlatformNavigationProvider : NavigationProvider
    {
        public override void SetNavigation(INavigationProviderContext context)
        {
            context.Manager.MainMenu
                .AddItem(
                    new MenuItemDefinition(
                        "Manage_System",
                        L("Manage_System"),
                        "fa fa-gear",
                        order: 99
                        ).AddItem(
                            new MenuItemDefinition(
                                "Manage_ChangePwd",
                                L("Manage_ChangePwd"),
                                url: "#/manage_changePwd",
                                requiresAuthentication: true,
                                requiredPermissionName: PermissionNames.Pages_Users,
                                icon: "fa fa-key"
                                )
                        ).AddItem(
                            new MenuItemDefinition(
                                "Manage_AuditLog",
                                L("Manage_AuditLog"),
                                url: "#/manage_auditLog",
                                requiresAuthentication: true,
                                requiredPermissionName: PermissionNames.Platform,
                                icon: "fa fa-eye"
                                )
                        ).AddItem(
                            new MenuItemDefinition(
                                "Manage_Syslog",
                                L("Manage_Syslog"),
                                url: "#/manage_syslog",
                                requiresAuthentication: true,
                                requiredPermissionName: PermissionNames.Platform,
                                icon: "fa fa-eye"
                                )
                        )
                )
                ;
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, PlatformConsts.LocalizationSourceName);
        }
    }
}
