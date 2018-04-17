(function () {
    "use strict";

    angular.module("app").config([
        "$ocLazyLoadProvider", function ($ocLazyLoadProvider) {
            $ocLazyLoadProvider.config({
                debug: true,
                events: true,
                modules: [
                    {
                        name: 'daterangepicker',
                        files: [
                            '/Scripts/angular-daterangepicker/daterangepicker.js',
                            '/Scripts/angular-daterangepicker/angular-daterangepicker.js'
                        ]
                    },
                    {
                        name: 'ui.select',
                        files: [
                            '/Scripts/angular-ui-select/select.css',
                            '/Scripts/angular-ui-select/select.js'
                        ]
                    },
                ]
            });
        }
    ]);
})();