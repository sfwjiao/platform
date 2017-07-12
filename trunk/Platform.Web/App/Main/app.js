(function () {
    "use strict";

    var app = angular.module("app", [
        "ngAnimate",
        "ngSanitize",

        "ui.router",
        "ui.bootstrap",
        "ui.jq",
        "ncy-angular-breadcrumb",
        "oc.lazyLoad",

        "abp",

        "ngCookies",
        "ngStorage"
    ]);

    app.config([
        "$controllerProvider", "$compileProvider", "$filterProvider", "$provide",
        function ($controllerProvider, $compileProvider, $filterProvider, $provide) {
            app.controller = $controllerProvider.register;
            app.directive = $compileProvider.directive;
            app.filter = $filterProvider.register;
            app.factory = $provide.factory;
            app.service = $provide.service;
            app.constant = $provide.constant;
            app.value = $provide.value;
        }
    ]);


    app.config(function ($breadcrumbProvider) {
        $breadcrumbProvider.setOptions({
            template: '<ul class="breadcrumb"><li><i class="fa fa-home"></i><a href="#">首页</a></li><li ng-repeat="step in steps" ng-class="{active: $last}" ng-switch="$last || !!step.abstract"><a ng-switch-when="false" href="{{step.ncyBreadcrumbLink}}">{{step.ncyBreadcrumbLabel}}</a><span ng-switch-when="true">{{step.ncyBreadcrumbLabel}}</span></li></ul>'
        });
    });

})();