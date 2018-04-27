using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace Platform.Authorization
{
    public class PlatformAuthorizationProvider : AuthorizationProvider
    {
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            //Common permissions
            var pages = context.GetPermissionOrNull(PermissionNames.Pages);
            if (pages == null)
            {
                pages = context.CreatePermission(PermissionNames.Pages, L("Pages"));
            }

            var users = pages.CreateChildPermission(PermissionNames.Pages_Users, L("Users"));
                        
            var tenants = pages.CreateChildPermission(PermissionNames.Pages_Tenants, L("Tenants"));
            
            //Host permissions
            var platform = pages.CreateChildPermission(PermissionNames.Platform, L("Platform"), multiTenancySides: MultiTenancySides.Host);
    }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, PlatformConsts.LocalizationSourceName);
        }
    }
}
