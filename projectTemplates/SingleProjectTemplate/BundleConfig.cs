using System.Web.Optimization;

namespace $projectname$.Api
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            var bundle = bundles.GetBundleFor("~/Bundles/App/Main/plugins/js");
            if (bundle == null)
            {
                bundle = new ScriptBundle("~/Bundles/App/Main/plugins/js");
                bundles.Add(bundle);
            }
            bundle.Include("~/Plugins/$projectname$/$projectname$.js");
            bundle.Include("~/Plugins/$projectname$/$projectname$ConfigRouter.js");
        }
    }
}
