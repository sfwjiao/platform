using Abp.Extensions;

namespace Platform.Extensions
{
    public static class StringExtensions
    {
        public static string WithDefaultValue(this string str, string defaultValue)
        {
            return str.IsNullOrEmpty() ? defaultValue : str;
        }
    }
}
