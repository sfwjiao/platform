using System.Web.Optimization;

namespace Platform.Web
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.IgnoreList.Clear();

            bundles.Add(
                new StyleBundle("~/Bundles/App/vendor/css")
                .Include("~/Content/bootstrap.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/font-awesome.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/weather-icons.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/typicons.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/animate.min.css", new CssRewriteUrlTransform())

                .Include("~/Content/themes/beyond/beyond.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/themes/beyond/demo.min.css", new CssRewriteUrlTransform())
                .Include("~/Content/themes/beyond/paper-edit.css", new CssRewriteUrlTransform())
                );

            bundles.Add(
                new ScriptBundle("~/Bundles/App/vendor/js")
                    .Include(
                        "~/Abp/Framework/scripts/utils/ie10fix.js",
                        "~/Scripts/json2.min.js",

                        "~/Scripts/modernizr-2.8.3.js",

                        "~/Scripts/jquery-2.2.0.min.js",
                        "~/Scripts/jquery-ui-1.11.4.min.js",

                        "~/Scripts/bootstrap.min.js",

                        "~/Scripts/moment-with-locales.min.js",
                        "~/Scripts/jquery.validate.min.js",
                        "~/Scripts/jquery.blockUI.js",
                        "~/Scripts/toastr.min.js",
                        "~/Scripts/others/spinjs/spin.js",
                        "~/Scripts/others/spinjs/jquery.spin.js",
                        "~/Scripts/slimscroll/jquery.slimscroll.min.js",
                        "~/Scripts/bootbox/bootbox.js",

                        "~/Scripts/angular.min.js",
                        "~/Scripts/angular-animate.min.js",
                        "~/Scripts/angular-sanitize.min.js",
                        "~/Scripts/angular-ui-router.min.js",
                        "~/Scripts/angular-ui/ui-bootstrap.min.js",
                        "~/Scripts/angular-ui/ui-bootstrap-tpls.min.js",
                        "~/Scripts/angular-ui/ui-utils.min.js",
                        "~/Scripts/angular-cookies.min.js",
                        "~/Scripts/angular-ngStorage/ngStorage.js",
                        "~/Scripts/angular-breadcrumb/angular-breadcrumb.js",
                        "~/Scripts/angular-ocLazyLoad/ocLazyLoad.js",

                        "~/Abp/Framework/scripts/abp.js",
                        "~/Abp/Framework/scripts/libs/abp.moment.js",
                        "~/Abp/Framework/scripts/libs/abp.bootbox.js",
                        "~/Abp/Framework/scripts/libs/abp.jquery.js",
                        "~/Abp/Framework/scripts/libs/abp.toastr.js",
                        "~/Abp/Framework/scripts/libs/abp.blockUI.js",
                        "~/Abp/Framework/scripts/libs/abp.spin.js",
                        "~/Abp/Framework/scripts/libs/angularjs/abp.ng.js",

                        "~/Scripts/beyond/skins.min.js",

                        "~/Scripts/jquery.signalR-2.2.1.min.js"
                    )
                );

            bundles.Add(
                new ScriptBundle("~/Bundles/App/Common/js")
                    .IncludeDirectory("~/Common/Scripts", "*.js", true)
                );

            var bundle = bundles.GetBundleFor("~/Bundles/App/Main/js");
            if (bundle == null)
            {
                bundle = new ScriptBundle("~/Bundles/App/Main/js");
                bundles.Add(bundle);
            }
            bundle
                .IncludeDirectory("~/App/Main", "*.js", false)
                .IncludeDirectory("~/App/Main/directives", "*.js", true)
                .IncludeDirectory("~/App/Main/filter", "*.js", true)
                .IncludeDirectory("~/App/Main/services", "*.js", true);
        }
    }
}