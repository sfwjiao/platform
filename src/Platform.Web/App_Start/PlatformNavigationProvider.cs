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
                                icon: "fa fa-key"
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
