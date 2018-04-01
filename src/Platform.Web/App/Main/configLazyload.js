(function () {
    "use strict";

    angular.module("app").config([
        "$ocLazyLoadProvider", function ($ocLazyLoadProvider) {
            $ocLazyLoadProvider.config({
                debug: true,
                events: true,
                modules: [
                    //{
                    //    name: "ngFileUpload",
                    //    files: [
                    //        "/Scripts/ng-file-upload/ng-file-upload-all.min.js"
                    //    ]
                    //}
                ]
            });
        }
    ]);
})();