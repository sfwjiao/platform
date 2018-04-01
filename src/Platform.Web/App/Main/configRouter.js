(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
	    "$stateProvider", "$urlRouterProvider",
	    function ($stateProvider, $urlRouterProvider) {
	        $urlRouterProvider.otherwise("/");

	        $stateProvider
	            .state("manage_changePwd", {
	                url: "/manage_changePwd",
	                templateUrl: "/App/Main/views/manage/changePwd.cshtml",
	                menu: "Manage_ChangePwd",
	                ncyBreadcrumb: {
	                    label: App.localize("Manage_ChangePwd"),
	                    description: App.localize("Manage_ChangePwdDescription")
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/App/Main/views/manage/changePwd.js"
							        ]
							    });
							}
	                    ]
	                }
	            })
	        ;

	        $urlRouterProvider.otherwise(abp.setting.get("Platform.ApplicationConfig.HomePageUrl"));
	    }
    ]);
})();