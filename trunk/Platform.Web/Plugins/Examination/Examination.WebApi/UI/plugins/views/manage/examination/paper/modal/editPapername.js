(function () {
    var controllerId = "app.views.manage.examination.paper.modal.editPaper";
    angular.module("app")
        .controller(controllerId,
        [
            "$scope",
            "appSession",
            "$state",
            "$http",
            "$stateParams",
            "$uibModal",
            "$uibModalInstance",
            "abp.services.app.screenings",
            "papers",
            function (
                $scope,
                appSession,
                $state,
                $http,
                $stateParams,
                $uibModal,
                $uibModalInstance,
                screeningsService,
                papers
            ) {


                //初始化
                $scope.init = function () {
                    $scope.title = "编辑考试信息";

                    $scope.paperin = papers;
                };
                $scope.init();
                $scope.save = function () {
                    $uibModalInstance.close($scope.paperin);
                }
                $scope.cancel = function () {
                    $uibModalInstance.dismiss();
                };

                //选择科目对话框
                $scope.selectSubjectUnit = function () {
                    var modalInstance = $uibModal.open({
                        templateUrl: "/App/Main/views/manage/trialBalloon/modal/subjectUnitSelectModal.cshtml",
                        controller: "app.views.manage.trialBalloon.modal.selectSubjectUnit as vm",
                        backdrop: "static"
                    });

                    modalInstance.result.then(function (selectedSubjectUnit) {
                        console.log(selectedSubjectUnit);
                        $scope.paperin.subjectUnitId = selectedSubjectUnit.id;
                        $scope.paperin.subjectUnit = selectedSubjectUnit;
                    });
                };

            }
        ]);
}
)();
