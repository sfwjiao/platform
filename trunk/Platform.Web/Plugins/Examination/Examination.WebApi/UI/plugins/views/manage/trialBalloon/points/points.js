(function () {
    var controllerId = "plugins.views.manage.trialBalloon.points";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.exam.points",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            pointsService
            ) {

            $scope.points = [];

            //初始化
            $scope.init = function () {
                pointsService.query({ pageSize: 0, start: 0 }).success(function (data) {
                    console.log(data);
                    $scope.points = data.items;
                });
            };

            //添加科目
            $scope.addPoints = function (parentPoints) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/plugins/views/manage/trialBalloon/points/modal/pointsDetailsModal.cshtml",
                    controller: "plugins.views.manage.trialBalloon.points.modal.addPoints",
                    backdrop: "static",
                    resolve: {
                        parentPoints: function () {
                            return angular.copy(parentPoints);
                        }
                    }
                });

                //对话框回调
                modalInstance.result.then(function (point) {
                    pointsService.addAndGetObj(point).success(function (data) {
                        if (parentPoints) {
                            if (!parentPoints.children) parentPoints.children = new Array();
                            parentPoints.children.push(data);
                        } else {
                            $scope.points.push(data);
                        }
                    });
                });
            };

            //编辑科目
            $scope.editPoints = function (point) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/plugins/views/manage/trialBalloon/points/modal/pointsDetailsModal.cshtml",
                    controller: "plugins.views.manage.trialBalloon.points.modal.editPoints",
                    backdrop: "static",
                    resolve: {
                        point: function () {
                            return angular.copy(point);
                        }
                    }
                });

                //对话框回调
                modalInstance.result.then(function (newPoints) {
                    pointsService.edit(newPoints).success(function () {
                        point.pointsContent = newPoints.pointsContent;
                    });
                });
            };

            //删除科目
            $scope.deletePoints = function (point, points) {
                abp.message.confirm("确认删除知识点\"" + point.pointsContent + "\"", function (isconfirm) {
                    if (isconfirm) {
                        pointsService.delete(point.id).success(function () {
                            abp.utils.removeArrayItem(point, points);
                        });
                    }
                });
            };

            $scope.init();
        }
    ]);
})();