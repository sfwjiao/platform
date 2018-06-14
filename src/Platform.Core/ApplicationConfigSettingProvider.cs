using System.Collections.Generic;
using Abp.Configuration;
using Abp.Localization;
using Platform.Users;

namespace Platform
{
    public  class ApplicationConfigSettingProvider : SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.WebName, 
                    "后台管理系统", 
                    L("WebName"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.PojectName, 
                    "后台管理系统", 
                    L("PojectName"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.LogoUrl,
                    "/logo.png",
                    L("LogoUrl"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.FaviconUrl,
                    "/favicon.png",
                    L("FaviconUrl"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CoverUrl, 
                    "/cover.png", 
                    L("CoverUrl"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyName,
                    "XXXXXX科技有限公司",
                    L("CompanyName"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyAddress, 
                    "河南省新乡市", 
                    L("CompanyAddress"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyTel, 
                    "0373-XXXXXXXX", 
                    L("CompanyTel"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyFax,
                    "0373-XXXXXXXX", 
                    L("CompanyFax"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyEmail, 
                    "53590040@qq.com", 
                    L("CompanyEmail"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyContacts, 
                    "深蓝", 
                    L("CompanyContacts"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.CompanyCopyright, 
                    "<a href='http://www.winmsoft.com' style='font-size: 11px'>XXXXXX科技有限公司</a>", 
                    L("CompanyCopyright"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.DefaultTenantAdminName, 
                    User.DefaultTenantAdminName, 
                    L("DefaultTenantAdminName"), 
                    scopes: SettingScopes.Application, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.DefaultTenantAdminLoginId,
                    User.DefaultTenantAdminLoginId,
                    L("DefaultTenantAdminLoginId"), 
                    scopes: SettingScopes.Application, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.DefaultTenantAdminPassword,
                    User.DefaultTenantAdminPassword,
                    L("DefaultTenantAdminPassword"), 
                    scopes: SettingScopes.Application, 
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.UserAvatarUrl,
                    "/Content/themes/beyond/images/defaultAvatar.jpg",
                    L("UserAvatarUrl"), 
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.HomePageUrl,
                    "/manage_changePwd",
                    L("HomePageUrl"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.UploadFilePath,
                    "~/Upload",
                    L("UploadFilePath"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.UploadFileAllowedExtensions,
                    "doc|docx|xsl|xslt",
                    L("UploadFileAllowedExtensions"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.UploadImagePath,
                    "~/UploadImg",
                    L("UploadImagePath"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    isVisibleToClients: true),
                new SettingDefinition(
                    PlatformConsts.ApplicationConfigSettingNames.UploadImageAllowedExtensions,
                    "jpg|png|gif",
                    L("UploadImageAllowedExtensions"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    isVisibleToClients: true),
            };
        }

        private static LocalizableString L(string name)
        {
            return new LocalizableString(name, PlatformConsts.LocalizationSourceName);
        }
    }
}
