(function () {
    //添加题库控制器
    angular.module("app").controller("app.views.manage.testing.modal.addExaminee", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$stateParams",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $stateParams
            ) {
            console.log($stateParams);
            $scope.title = "添加考生";
            $scope.examinee = {
                department: "",
                className: "",
                number: "",
                name: "",
                gender: "",
                examId: $stateParams.id
            };


            $scope.init = function () {
            };

            //提交事件
            $scope.save = function () {
                console.log($scope.examinee);
                $uibModalInstance.close($scope.examinee);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

        }
    ]);
})();