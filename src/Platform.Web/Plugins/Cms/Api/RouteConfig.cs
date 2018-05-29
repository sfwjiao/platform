using System.Web.Routing;
using Abp.Dependency;

namespace Cms.Api
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes, IIocManager iocManage)
        {
            routes.Add("Cms", new CmsRoute(iocManage));
        }
    }
}
