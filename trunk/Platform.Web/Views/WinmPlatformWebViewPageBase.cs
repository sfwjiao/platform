using Abp.Configuration;
using Abp.Web.Mvc.Views;
using Platform;

namespace Platform.Web.Views
{
    public abstract class PlatformWebViewPageBase : PlatformWebViewPageBase<dynamic>
    {

    }

    public abstract class PlatformWebViewPageBase<TModel> : AbpWebViewPage<TModel>
    {
        protected PlatformWebViewPageBase()
        {
            LocalizationSourceName = PlatformConsts.LocalizationSourceName;
        }

        public string Setting(string name)
        {
            return SettingManager.GetSettingValue(name);
        }

        public string AppSetting(string name)
        {
            return SettingManager.GetSettingValueForApplication(name);
        }
    }
}