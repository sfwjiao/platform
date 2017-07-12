(function () {
    //添加题库控制器
    angular.module("app").controller("app.views.manage.testing.modal.editExaminee", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "library",
        function (
             $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            library
            ) {

            $scope.title = "编辑考生信息";
            $scope.examinee = library;


            $scope.init = function () {
            };

            //提交事件
            $scope.save = function () {

                $uibModalInstance.close($scope.examinee);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

        }
    ]);
})();