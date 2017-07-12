(function () {
    //添加科目控制器
    angular.module("app").controller("plugins.views.manage.trialBalloon.modal.addSubjectUnit", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "parentSubjectUnit",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            parentSubjectUnit
            ) {

            $scope.title = "添加科目";
            $scope.subjectUnit = {

            };

            $scope.init = function () {
                if (parentSubjectUnit) {
                    $scope.subjectUnit.parentId = parentSubjectUnit.id;
                }
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.subjectUnit);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);

    //修改科目控制器
    angular.module("app").controller("plugins.views.manage.trialBalloon.modal.editSubjectUnit", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "subjectUnit",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            subjectUnit
            ) {

            $scope.title = "修改科目";
            $scope.subjectUnit = {};

            $scope.init = function () {
                $scope.subjectUnit = subjectUnit;
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.subjectUnit);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();