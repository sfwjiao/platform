using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Localization;
using Abp.Organizations;
using Abp.Runtime.Caching;
using Microsoft.AspNet.Identity;
using Platform.Authorization.Roles;

namespace Platform.Users
{
    public class UserManager : AbpUserManager<Role, User>
    {
        protected ISettingManager SettingManager { get; }

        public UserManager(
            UserStore userStore,
            RoleManager roleManager,
            IPermissionManager permissionManager,
            IUnitOfWorkManager unitOfWorkManager,
            ICacheManager cacheManager,
            IRepository<OrganizationUnit, long> organizationUnitRepository,
            IRepository<UserOrganizationUnit, long> userOrganizationUnitRepository,
            IOrganizationUnitSettings organizationUnitSettings,
            ILocalizationManager localizationManager,
            ISettingManager settingManager,
            IdentityEmailMessageService emailService,
            IUserTokenProviderAccessor userTokenProviderAccessor)
            : base(
                  userStore,
                  roleManager,
                  permissionManager,
                  unitOfWorkManager,
                  cacheManager,
                  organizationUnitRepository,
                  userOrganizationUnitRepository,
                  organizationUnitSettings,
                  localizationManager,
                  emailService,
                  settingManager,
                  userTokenProviderAccessor)
        {
            SettingManager = settingManager;
        }

        public async Task<User> CreateDefaultTenantAdminUserAsync(int tenantId, string emailAddress, string password)
        {
            var adminName = await SettingManager.GetSettingValueForApplicationAsync(PlatformConsts.ApplicationConfigSettingNames.DefaultTenantAdminName);

            var loginId = await SettingManager.GetSettingValueForApplicationAsync(PlatformConsts.ApplicationConfigSettingNames.DefaultTenantAdminLoginId);

            if (string.IsNullOrEmpty(password))
            {
                password = await SettingManager.GetSettingValueForApplicationAsync(PlatformConsts.ApplicationConfigSettingNames.DefaultTenantAdminPassword);
            }
            return new User
            {
                TenantId = tenantId,
                UserName = loginId,
                Name = adminName,
                Surname = adminName,
                EmailAddress = emailAddress,
                Password = new PasswordHasher().HashPassword(password)
            };
        }
    }
}