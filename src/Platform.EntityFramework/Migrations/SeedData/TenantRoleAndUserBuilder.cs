using System.Linq;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.MultiTenancy;
using Microsoft.AspNet.Identity;
using Platform.Authorization;
using Platform.Authorization.Roles;
using Platform.EntityFramework;
using Platform.Users;

namespace Platform.Migrations.SeedData
{
    public class TenantRoleAndUserBuilder
    {
        private readonly PlatformDbContext _context;
        private readonly int _tenantId;

        public TenantRoleAndUserBuilder(PlatformDbContext context, int tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            CreateRolesAndUsers();
        }

        private void CreateRolesAndUsers()
        {
            //Admin role

            var adminRole = _context.Roles.FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Admin);
            if (adminRole == null)
            {
                adminRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Admin, StaticRoleNames.Tenants.Admin) { IsStatic = true });
                _context.SaveChanges();

                //Grant all permissions to admin role
                var permissions = PermissionFinder
                    .GetAllPermissions(new PlatformAuthorizationProvider())
                    .Where(p => p.MultiTenancySides.HasFlag(MultiTenancySides.Tenant))
                    .ToList();

                foreach (var permission in permissions)
                {
                    _context.Permissions.Add(
                        new RolePermissionSetting
                        {
                            TenantId = _tenantId,
                            Name = permission.Name,
                            IsGranted = true,
                            RoleId = adminRole.Id
                        });
                }

                _context.SaveChanges();
            }

            //admin user
            var adminUser = _context.Users.FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == User.DefaultTenantAdminLoginId);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    TenantId = _tenantId,
                    UserName = User.DefaultTenantAdminLoginId,
                    Name = User.DefaultTenantAdminName,
                    Surname = User.DefaultTenantAdminName,
                    EmailAddress = User.DefaultTenantAdminEmail,
                    IsEmailConfirmed = true,
                    Password = new PasswordHasher().HashPassword(User.DefaultTenantAdminPassword)
                };
                adminUser.IsEmailConfirmed = true;
                adminUser.IsActive = true;

                _context.Users.Add(adminUser);
                _context.SaveChanges();

                //Assign Admin role to admin user
                _context.UserRoles.Add(new UserRole(_tenantId, adminUser.Id, adminRole.Id));
                _context.SaveChanges();
            }
        }
    }
}