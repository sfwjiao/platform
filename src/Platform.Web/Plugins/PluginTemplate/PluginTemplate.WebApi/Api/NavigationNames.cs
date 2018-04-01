namespace PluginTemplate.Api
{
    public class NavigationNames
    {
        public const string Custom = "Custom";
        public const string CustomCustomer = "Custom_Customer";

        public static class Propertys
        {
            public const string Order = "Order";
            public const string IsVisible = "IsVisible";
            public const string IsEnabled = "IsEnabled";
            public const string RequiredPermissionName = "RequiredPermissionName";
            public const string Icon = "Icon";
            public const string Url = "Url";
        }

        public static string GetName(string nav, string property)
            => $"{PluginTemplateConsts.LocalizationSourceName}.{nav}.{property}";
    }
}
