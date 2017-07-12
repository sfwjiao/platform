(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
	    "$stateProvider", "$urlRouterProvider",
	    function ($stateProvider, $urlRouterProvider) {

	        $stateProvider
	            .state("custom_customer", {
	                url: "/custom_customer",
	                templateUrl: "/App/Main/plugins/views/custom/customer.cshtml",
	                menu: "Custom_Customer",
	                ncyBreadcrumb: {
	                    label: "客户管理",
	                    description: "客户信息列表"
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/App/Main/plugins/views/custom/customer.js"
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