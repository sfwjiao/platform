using System.Linq;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.MultiTenancy;
using Platform.Authorization;
using Platform.Authorization.Roles;
using Platform.EntityFramework;
using Platform.Users;
using Microsoft.AspNet.Identity;

namespace Platform.Migrations.SeedData
{
    public class HostRoleAndUserCreator
    {
        private readonly PlatformDbContext _context;

        public HostRoleAndUserCreator(PlatformDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            CreateHostRoleAndUsers();
        }

        private void CreateHostRoleAndUsers()
        {
            //Admin role for host

            var adminRoleForHost = _context.Roles.FirstOrDefault(r => r.TenantId == null && r.Name == StaticRoleNames.Host.Admin);
            if (adminRoleForHost == null)
            {
                adminRoleForHost = _context.Roles.Add(new Role { Name = StaticRoleNames.Host.Admin, DisplayName = StaticRoleNames.Host.Admin, IsStatic = true });
                _context.SaveChanges();

                //Grant all tenant permissions
                var permissions = PermissionFinder
                    .GetAllPermissions(new PlatformAuthorizationProvider())
                    .Where(p => p.MultiTenancySides.HasFlag(MultiTenancySides.Host))
                    .ToList();

                foreach (var permission in permissions)
                {
                    _context.Permissions.Add(
                        new RolePermissionSetting
                        {
                            Name = permission.Name,
                            IsGranted = true,
                            RoleId = adminRoleForHost.Id
                        });
                }

                _context.SaveChanges();
            }

            //Admin user for tenancy host

            var adminUserForHost = _context.Users.FirstOrDefault(u => u.TenantId == null && u.UserName == "sfwjiao");
            if (adminUserForHost == null)
            {
                adminUserForHost = _context.Users.Add(
                    //TODO:超级管理员信息
                    new User
                    {
                        UserName = "sfwjiao",
                        Name = "超级管理员",
                        Surname = "超级管理员",
                        EmailAddress = "isaac.joy.cn@hotmail.com",
                        IsEmailConfirmed = true,
                        Password = new PasswordHasher().HashPassword("rxf021477")
                    });

                _context.SaveChanges();

                _context.UserRoles.Add(new UserRole(null, adminUserForHost.Id, adminRoleForHost.Id));

                _context.SaveChanges();
            }
        }
    }
}