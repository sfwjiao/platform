(function () {
    var controllerId = "examination.views.manage.trialBalloon.subjectUnit";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.exam.subjectUnit",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            subjectUnitService
            ) {

            $scope.subjectUnits = [];

            //初始化
            $scope.init = function () {
                subjectUnitService.query({ pageSize: 0, start: 0 }).success(function (data) {
                    console.log(data);
                    $scope.subjectUnits = data.items;
                });
            };

            //添加科目
            $scope.addSubjectUnit = function (parentSubjectUnit) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/plugins/views/manage/trialBalloon/modal/subjectUnitDetailsModal.cshtml",
                    controller: "plugins.views.manage.trialBalloon.modal.addSubjectUnit",
                    backdrop: "static",
                    resolve: {
                        parentSubjectUnit: function () {
                            return angular.copy(parentSubjectUnit);
                        }
                    }
                });

                //对话框回调
                modalInstance.result.then(function (newSubjectUnit) {
                    subjectUnitService.addAndGetObj(newSubjectUnit).success(function (data) {
                        if (parentSubjectUnit) {
                            if (!parentSubjectUnit.children) parentSubjectUnit.children = new Array();
                            parentSubjectUnit.children.push(data);
                        } else {
                            $scope.subjectUnits.push(data);
                        }
                    });
                });
            };

            //编辑科目
            $scope.editSubjectUnit = function (subjectUnit) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/plugins/views/manage/trialBalloon/modal/subjectUnitDetailsModal.cshtml",
                    controller: "plugins.views.manage.trialBalloon.modal.editSubjectUnit as vm",
                    backdrop: "static",
                    resolve: {
                        subjectUnit: function () {
                            return angular.copy(subjectUnit);
                        }
                    }
                });

                //对话框回调
                modalInstance.result.then(function (newSubjectUnit) {
                    subjectUnitService.edit(newSubjectUnit).success(function () {
                        subjectUnit.displayName = newSubjectUnit.displayName;
                    });
                });
            };

            //删除科目
            $scope.deleteSubjectUnit = function (subjectUnit, subjectUnits) {
                abp.message.confirm("确认删除科目\"" + subjectUnit.displayName + "\"", function (isconfirm) {
                    if (isconfirm) {
                        //TODO:从服务端删除科目,删除时服务端判断是否包含子科目，提示“包含子科目删除时将一同删除，是否删除？”
                        subjectUnitService.delete(subjectUnit.id).success(function() {
                            abp.utils.removeArrayItem(subjectUnit, subjectUnits);
                        });
                    }
                });
            };

            $scope.init();
        }
    ]);
})();