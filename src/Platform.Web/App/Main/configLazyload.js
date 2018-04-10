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
                        serie: true,
                        files: [
                            '/Scripts/angular-daterangepicker/daterangepicker.js',
                            '/Scripts/angular-daterangepicker/angular-daterangepicker.js'
                        ]
                    }
                ]
            });
        }
    ]);
})();