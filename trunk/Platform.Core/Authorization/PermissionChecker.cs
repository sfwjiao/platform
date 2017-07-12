using Abp.Authorization;
using Platform.Authorization.Roles;
using Platform.MultiTenancy;
using Platform.Users;

namespace Platform.Authorization
{
    public class PermissionChecker : PermissionChecker<Tenant, Role, User>
    {
        public PermissionChecker(UserManager userManager)
            : base(userManager)
        {

        }
    }
}
