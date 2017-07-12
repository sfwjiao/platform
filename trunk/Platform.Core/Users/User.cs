using System;
using Abp.Authorization.Users;
using Abp.Extensions;

namespace Platform.Users
{
    public class User : AbpUser<User>
    {
        public const string DefaultPassword = "888888";

        public const string DefaultTenantAdminName = "管理员";
        public const string DefaultTenantAdminLoginId = "admin";
        public const string DefaultTenantAdminPassword = "888888";
        public const string DefaultTenantAdminEmail = "isaac.joy.cn@hotmail.com";

        public static string CreateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Truncate(16);
        }
    }
}