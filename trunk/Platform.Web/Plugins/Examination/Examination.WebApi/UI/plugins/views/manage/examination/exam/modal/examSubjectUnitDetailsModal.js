(function () {
    //添加考试科目控制器
    angular.module("app").controller("app.views.manage.examination.exam.modal.addExamSubjectUnit", [
        "$scope",
        "$compile",
         "$state",
        "$uibModalInstance",
        "$uibModal",
        "examId",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            examId
            ) {

            $scope.title = "添加考试科目";

            $scope.examSubjectUnit = {};

            $scope.selectSubjectUnit = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/modal/subjectUnitSelectModal.cshtml",
                    controller: "app.views.manage.trialBalloon.modal.selectSubjectUnit as vm",
                    backdrop: "static"
                });

                modalInstance.result.then(function (selectedSubjectUnit) {
                    console.log(selectedSubjectUnit);
                    $scope.examSubjectUnit.subjectUnit = selectedSubjectUnit;
                    $scope.examSubjectUnit.subjectUnitId = selectedSubjectUnit.id;
                });
            };

            $scope.init = function () {
                $scope.examSubjectUnit.examId = examId;
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.examSubjectUnit);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
    //编辑考试科目控制器
    angular.module("app").controller("app.views.manage.examination.exam.modal.editExamSubjectUnit", [
        "$scope",
        "$compile",
         "$state",
        "$uibModalInstance",
        "$uibModal",
        "examSubjectUnit",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            examSubjectUnit
            ) {

            $scope.title = "编辑考试科目";

            $scope.examSubjectUnit = {};

            $scope.selectSubjectUnit = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/modal/subjectUnitSelectModal.cshtml",
                    controller: "app.views.manage.trialBalloon.modal.selectSubjectUnit as vm",
                    backdrop: "static"
                });

                modalInstance.result.then(function (selectedSubjectUnit) {
                    console.log(selectedSubjectUnit);
                    $scope.examSubjectUnit.subjectUnit = selectedSubjectUnit;
                    $scope.examSubjectUnit.subjectUnitId = selectedSubjectUnit.id;
                });
            };

            $scope.init = function () {
                $scope.examSubjectUnit = examSubjectUnit;
            };

            //提交事件
            $scope.save = function () {
                $uibModalInstance.close($scope.examSubjectUnit);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();