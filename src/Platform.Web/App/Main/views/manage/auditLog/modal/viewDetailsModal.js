(function () {
    angular.module("app").controller("app.views.manage.auditLog.modal.viewDetailsModal", [
        "$scope",
        "$uibModalInstance",
        "abp.services.app.auditLog",
        "auditLogId",
        function (
            $scope,
            $uibModalInstance,
            auditLogService,
            auditLogId
        ) {
            $scope.title = "审计日志";
            $scope.auditLog = {};

            $scope.init = function () {
                $scope.getData();
            };

            $scope.getData = function () {
                auditLogService.get(auditLogId).success(function (data) {
                    console.log(data);
                    $scope.auditLog = data;
                })
            };

            //提交事件
            //$scope.save = function () {
            //    $uibModalInstance.close($scope.data);
            //}

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.$on("click", function () {
                $uibModalInstance.dismiss();
            });

            $scope.init();
        }
    ]);
})();