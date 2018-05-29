using Abp.Domain.Services;

namespace Cms.Core.Route
{
    public class PageActionManager : IDomainService
    {
        public bool Exists(string pageName)
        {
            return pageName == "test";
        }
    }
}
