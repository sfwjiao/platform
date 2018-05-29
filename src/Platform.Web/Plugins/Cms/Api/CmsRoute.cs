using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Abp.Dependency;
using Abp.MultiTenancy;
using Cms.Core.Route;

namespace Cms.Api
{
    public class CmsRoute : RouteBase
    {
        private readonly ITenantResolver _tenantResolver;
        private readonly ITenantStore _tenantStore;
        private readonly PageActionManager _pageActionManager;

        public CmsRoute(IIocManager iocManage)
        {
            _tenantResolver = iocManage.Resolve<ITenantResolver>();
            _tenantStore = iocManage.Resolve<ITenantStore>();
            _pageActionManager = iocManage.Resolve<PageActionManager>();
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            var id = _tenantResolver.ResolveTenantId();
            if (!id.HasValue) return null;

            var tenant = _tenantStore.Find(id.Value);
            if (tenant == null) return null;
            
            if (!_pageActionManager.Exists(httpContext.Request.Path.TrimStart('/'))) return null;

            var routeData = new RouteData(this, new MvcRouteHandler());
            routeData.Values.Add("controller", "Cms");
            routeData.Values.Add("action", "Page");
            
            return routeData;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }
    }
}
