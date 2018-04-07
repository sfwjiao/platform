(function () {
    var controllerId = "app.views.manage.auditLog.index";
    angular.module("app").controller(controllerId, [
        "$scope", "$uibModal", "abp.services.app.auditLog", function ($scope, $uibModal, auditLogService) {
            $scope.auditLogs = [];

            $scope.init = function () {
                $scope.getData();
            };

            $scope.getData = function () {
                auditLogService.query({
                    pageSize: 20,
                    start: 0,
                    keyWord: ""
                }).success(function (data) {
                    console.log(data);
                    $scope.auditLogs = data.items;
                });
            };

            $scope.viewDetails = function (id) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/auditLog/modal/viewDetailsModal.cshtml",
                    controller: "app.views.manage.auditLog.modal.viewDetailsModal",
                    backdrop: "true",
                    resolve: {
                        deps: abp.utils.getDeps([
                            "/App/Main/views/manage/auditLog/modal/viewDetailsModal.js"
                        ]),
                        auditLogId: function () {
                            return angular.copy(id);
                        }
                    }
                });
            };

            $scope.init();
        }
    ]);
})();