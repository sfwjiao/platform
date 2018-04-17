(function () {
    var controllerId = "app.views.manage.auditLog.index";
    angular.module("app").controller(controllerId, [
        "$scope",
        "$uibModal",
        "queryFilterFactory",
        "abp.services.app.auditLog",
        function (
            $scope,
            $uibModal,
            queryFilterFactory,
            auditLogService
        ) {
            //日志数据
            $scope.auditLogs = [];

            //分页
            $scope.pagination = angular.extend({}, queryFilterFactory.pagination);
            //过滤
            $scope.filterOptions = angular.extend({
                date: {}
            }, queryFilterFactory.filterOptions);

            //监听页码，根据页码变化请求数据
            $scope.$watch("pagination.pageIndex", function (pageIndex) {
                $scope.getData();
            });

            //获取表单数据
            $scope.getData = function () {
                console.log($scope.filterOptions.date);
                auditLogService.query({
                    pageSize: $scope.pagination.pageSize,
                    start: $scope.pagination.start(),
                    keyWord: "",
                    startDate: $scope.filterOptions.date.startDate,
                    endDate: $scope.filterOptions.date.endDate,
                    serviceName: $scope.filterOptions.serviceName,
                    methodName: $scope.filterOptions.methodName
                }).success(function (data) {
                    console.log(data);
                    $scope.auditLogs = data.items;
                    $scope.pagination.totalCount = data.totalCount;
                });
            };

            //详情弹出框
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

            //初始化
            $scope.init = function () {
                var now = abp.clock.now();
                $scope.filterOptions.date = {
                    startDate: now,
                    endDate: now
                };

                $scope.getData();
            };

            $scope.init();
        }
    ]);
})();