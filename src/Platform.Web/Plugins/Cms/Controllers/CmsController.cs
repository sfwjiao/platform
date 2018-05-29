using System.Threading.Tasks;
using System.Web.Mvc;
using Abp.IdentityFramework;
using Abp.Threading;
using Abp.UI;
using Abp.Web.Mvc.Controllers;
using Microsoft.AspNet.Identity;

namespace Cms.Controllers
{
    public class CmsController : AbpController
    {
        public CmsController()
        {
            LocalizationSourceName = CmsConsts.LocalizationSourceName;
        }

        public ActionResult Content()
        {
            var str = RouteData.Values["cmsRouteValue"]?.ToString();
            return Content(str);
        }

        protected virtual void CheckModelState()
        {
            if (!ModelState.IsValid)
            {
                throw new UserFriendlyException(L("FormIsNotValidMessage"));
            }
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        public async Task<ActionResult> Page()
        {
            return await Task.FromResult(Content("success"));
        }
    }
}
