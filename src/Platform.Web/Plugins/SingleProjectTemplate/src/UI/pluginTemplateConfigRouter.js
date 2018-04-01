(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
	    "$stateProvider", "$urlRouterProvider",
	    function ($stateProvider, $urlRouterProvider) {

	        $stateProvider
	            .state("custom_customer", {
	                url: "/custom_customer",
	                templateUrl: "/Plugins/singleProjectTemplate/views/custom/customer.cshtml",
	                menu: "Custom_Customer",
	                ncyBreadcrumb: {
	                    label: plugin.SingleProjectTemplate.localize("Custom_Customer"),
	                    description: plugin.SingleProjectTemplate.localize("Custom_CustomerDescirption")
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/Plugins/singleProjectTemplate/views/custom/customer.js"
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