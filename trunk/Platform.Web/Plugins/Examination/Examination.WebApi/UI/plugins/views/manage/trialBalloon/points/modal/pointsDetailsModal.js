(function () {
    //添加科目控制器
    angular.module("app").controller("plugins.views.manage.trialBalloon.points.modal.addPoints", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "parentPoints",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            parentPoints
            ) {

            $scope.title = "添加知识点";
            $scope.points = {

            };

            $scope.init = function () {
                if (parentPoints) {
                    $scope.points.parentId = parentPoints.id;
                }
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.points);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);

    //修改科目控制器
    angular.module("app").controller("plugins.views.manage.trialBalloon.points.modal.editPoints", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "points",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            points
            ) {

            $scope.title = "修改知识点";
            $scope.points = {};

            $scope.init = function () {
                $scope.points = points;
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.points);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();