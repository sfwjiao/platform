(function () {
    //添加大题控制器
    angular.module("app").controller("app.views.manage.examination.paper.modal.addGroup", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "paper",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            paper
            ) {

            $scope.title = "添加大题";
            $scope.group = {

            };
            $scope.questionTypes = [];

            $scope.init = function () {
                $scope.group.examinationPaperId = paper.id;
                $scope.questionTypes = eval(App.localize("QuestionTypes"));
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.group);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
    //编辑大题控制器
    angular.module("app").controller("app.views.manage.examination.paper.modal.editGroup", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "paper",
        "group",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            paper,
            group
            ) {

            $scope.title = "编辑大题";
            $scope.group = {};
            $scope.questionTypes = [];

            $scope.init = function () {
                $scope.group = group;
                $scope.questionTypes = eval(App.localize("QuestionTypes"));
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.group);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();