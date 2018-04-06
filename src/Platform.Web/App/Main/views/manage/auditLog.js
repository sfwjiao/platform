(function () {
    var controllerId = "app.views.manage.auditLog";
    angular.module("app").controller(controllerId, [
        "$scope", "abp.services.app.auditLog", function ($scope, auditLogService) {
            $scope.auditLogs = [];

            $scope.init = function() {
                $scope.getData();
            };

            $scope.getData = function() {
                auditLogService.query({
                    pageSize: 20,
                    start: 0,
                    keyWord: ""
                }).success(function(data) {
                    console.log(data);
                    $scope.auditLogs = data.items;
                });
            };

            $scope.init();
        }
    ]);
})();