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
                .state("manage_AuditLog", {
                    url: "/manage_auditLog",
                    templateUrl: "/App/Main/views/manage/auditLog.cshtml",
                    menu: "Manage_AuditLog",
	                ncyBreadcrumb: {
	                    label: App.localize("Manage_AuditLog"),
	                    description: App.localize("Manage_AuditLogDescription")
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/App/Main/views/manage/auditLog.js"
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