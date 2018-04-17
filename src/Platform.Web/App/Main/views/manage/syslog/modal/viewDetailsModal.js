(function () {
    angular.module("app").controller("app.views.manage.syslog.modal.viewDetailsModal", [
        "$scope",
        "$uibModalInstance",
        "abp.services.app.syslog",
        "syslogId",
        function (
            $scope,
            $uibModalInstance,
            syslogService,
            syslogId
        ) {
            $scope.title = "系统日志";
            $scope.syslog = {};

            $scope.init = function () {
                $scope.getData();
            };

            $scope.getData = function () {
                syslogService.get(syslogId).success(function (data) {
                    console.log(data);
                    $scope.syslog = data;
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