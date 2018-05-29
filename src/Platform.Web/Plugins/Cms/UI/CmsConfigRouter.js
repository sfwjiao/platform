(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
	    "$stateProvider", "$urlRouterProvider",
	    function ($stateProvider, $urlRouterProvider) {

	        $stateProvider
	            .state("Cms_manage", {
	                url: "/Cms_manage",
	                templateUrl: "/Plugins/Cms/views/Cms/index.cshtml",
	                menu: "Cms_Manage",
	                ncyBreadcrumb: {
                        label: plugin.Cms.localize("Cms_Manage"),
                        description: plugin.Cms.localize("Cms_ManageDescirption")
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/Plugins/Cms/views/Cms/index.js"
							        ]
							    });
							}
	                    ]
	                }
	            })
	        ;
	    }
    ]);
})();