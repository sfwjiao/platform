using System;
using System.IO;
using System.Web;
using System.Web.Hosting;
using Abp.Castle.Logging.Log4Net;
using Abp.PlugIns;
using Abp.Web;
using Castle.Facilities.Logging;
using Platform.Web;

[assembly: PreApplicationStartMethod(typeof(PreStarter), "Start")]
namespace Platform.Web
{
    public class MvcApplication : AbpWebApplication<PlatformWebModule>
    {
        protected override void Application_Start(object sender, EventArgs e)
        {
            AbpBootstrapper.IocManager.IocContainer.AddFacility<LoggingFacility>(
                f => f.UseAbpLog4Net().WithConfig("log4net.config")
            );

            base.Application_Start(sender, e);
        }
    }

    public static class PreStarter
    {
        public static void Start()
        {
            //MvcApplication.AbpBootstrapper.PlugInSources.AddFolder(
            //    HostingEnvironment.MapPath("~/Plugins/SingleProjectTemplate/release"));
            //MvcApplication.AbpBootstrapper.PlugInSources.AddFolder(
            //    HostingEnvironment.MapPath("~/Plugins/FileUpload/release"));

            //MvcApplication.AbpBootstrapper.PlugInSources.AddToBuildManager();
        }
    }
}
