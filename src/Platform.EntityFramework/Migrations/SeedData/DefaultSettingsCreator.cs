using System.Linq;
using Abp.Configuration;
using Abp.Localization;
using Abp.Net.Mail;
using Platform.EntityFramework;

namespace Platform.Migrations.SeedData
{
    public class DefaultSettingsCreator
    {
        private readonly PlatformDbContext _context;

        public DefaultSettingsCreator(PlatformDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            //TODO:修改应用程序配置
            //Emailing
            AddSettingIfNotExists(EmailSettingNames.DefaultFromAddress, "isaac.joy.cn@hotmail.com");
            AddSettingIfNotExists(EmailSettingNames.DefaultFromDisplayName, "维迈网络 焦晓辉");

            //Languages
            AddSettingIfNotExists(LocalizationSettingNames.DefaultLanguage, "zh-CN");
        }

        private void AddSettingIfNotExists(string name, string value, int? tenantId = null)
        {
            if (_context.Settings.Any(s => s.Name == name && s.TenantId == tenantId && s.UserId == null))
            {
                return;
            }

            _context.Settings.Add(new Setting(tenantId, null, name, value));
            _context.SaveChanges();
        }
    }
}