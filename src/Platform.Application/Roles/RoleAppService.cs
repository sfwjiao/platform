using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Platform.Authorization;
using Platform.Authorization.Roles;
using Platform.Roles.Dto;

namespace Platform.Roles
{
    [AbpAuthorize(PermissionNames.Pages_Tenants)]
    public class RoleAppService : PlatformAppServiceBase,IRoleAppService
    {
        private readonly RoleManager _roleManager;
        private readonly IPermissionManager _permissionManager;

        public RoleAppService(RoleManager roleManager, IPermissionManager permissionManager)
        {
            _roleManager = roleManager;
            _permissionManager = permissionManager;
        }

        public async Task UpdateRolePermissions(UpdateRolePermissionsInput input)
        {
            var role = await _roleManager.GetRoleByIdAsync(input.RoleId);
            var grantedPermissions = _permissionManager
                .GetAllPermissions()
                .Where(p => input.GrantedPermissionNames.Contains(p.Name))
                .ToList();

            await _roleManager.SetGrantedPermissionsAsync(role, grantedPermissions);
        }
    }
}