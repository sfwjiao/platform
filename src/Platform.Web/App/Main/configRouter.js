(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
        "$stateProvider", "$urlRouterProvider",
        function ($stateProvider, $urlRouterProvider) {
            $urlRouterProvider.otherwise("/");

            if (abp.auth.hasPermission("Platform")) {
                $stateProvider
                    .state("manage_AuditLog", {
                        url: "/manage_auditLog",
                        templateUrl: "/App/Main/views/manage/auditLog/index.cshtml",
                        menu: "Manage_AuditLog",
                        ncyBreadcrumb: {
                            label: App.localize("Manage_AuditLog"),
                            description: App.localize("Manage_AuditLogDescription")
                        },
                        resolve: {
                            deps: [
                                "$ocLazyLoad",
                                function ($ocLazyLoad) {
                                    return $ocLazyLoad.load(['daterangepicker']).then(
                                        function () {
                                            return $ocLazyLoad.load(
                                                {
                                                    serie: true,
                                                    files: [
                                                        "/App/Main/factory/queryFilterFactory.js",
                                                        "/App/Main/views/manage/auditLog/index.js"
                                                    ]
                                                });
                                        }
                                    );
                                }
                            ]
                        }
                    })
                    .state("manage_Syslog", {
                        url: "/manage_syslog",
                        templateUrl: "/App/Main/views/manage/syslog/index.cshtml",
                        menu: "Manage_Syslog",
                        ncyBreadcrumb: {
                            label: App.localize("Manage_Syslog"),
                            description: App.localize("Manage_SyslogDescription")
                        },
                        resolve: {
                            deps: [
                                "$ocLazyLoad",
                                function ($ocLazyLoad) {
                                    return $ocLazyLoad.load(['daterangepicker', 'ui.select']).then(
                                        function () {
                                            return $ocLazyLoad.load(
                                                {
                                                    serie: true,
                                                    files: [
                                                        "/App/Main/factory/queryFilterFactory.js",
                                                        "/App/Main/views/manage/syslog/index.js"
                                                    ]
                                                }
                                            );
                                        }
                                    );
                                }
                            ]
                        }
                    })
                    ;
            }
            if (abp.auth.hasPermission("Pages.Users")) {
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
            }

            $urlRouterProvider.otherwise(abp.setting.get("Platform.ApplicationConfig.HomePageUrl"));
        }
    ]);
})();