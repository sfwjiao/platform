using Xunit;

namespace PluginTemplate.Tests
{
    public sealed class MultiTenantFactAttribute : FactAttribute
    {
        public MultiTenantFactAttribute()
        {
            //if (!PluginTemplateConsts.MultiTenancyEnabled)
            //{
            //    Skip = "MultiTenancy is disabled.";
            //}
        }
    }
}
