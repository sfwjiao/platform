(function () {
    //选择科目控制器
    angular.module("app").controller("plugins.views.manage.trialBalloon.modal.selectSubjectUnit", [
        "$scope",
        "$compile",
        "$state",
        "$http",
        "$uibModalInstance",
        "abp.services.exam.subjectUnit",
        function (
            $scope,
            $compile,
            $state,
            $http,
            $uibModalInstance,
            subjectUnitService
            ) {

            $scope.title = "选择科目";
            $scope.subjectUnits = [];

            $scope.init = function () {
                console.log({ pageSize: 0, start: 0 });
                subjectUnitService.query({ pageSize: 0, start: 0 }).success(function (data) {
                    $scope.subjectUnits = data.items;
                });
            };

            //提交事件
            $scope.selectSubjectUnit = function (subjectUnit) {
                $uibModalInstance.close(subjectUnit);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();