(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
	    "$stateProvider", "$urlRouterProvider",
	    function ($stateProvider, $urlRouterProvider) {

	        $stateProvider
	            .state("$projectname$_manage", {
	                url: "/$projectname$_manage",
	                templateUrl: "/Plugins/$projectname$/views/$projectname$/index.cshtml",
	                menu: "$projectname$_Manage",
	                ncyBreadcrumb: {
                        label: plugin.$projectname$.localize("$projectname$_Manage"),
                        description: plugin.$projectname$.localize("$projectname$_ManageDescirption")
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/Plugins/$projectname$/views/$projectname$/index.js"
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