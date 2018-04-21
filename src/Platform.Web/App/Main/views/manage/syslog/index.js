(function () {
    var controllerId = "app.views.manage.syslog.index";
    angular.module("app").controller(controllerId, [
        "$scope",
        "$uibModal",
        "queryFilterFactory",
        "abp.services.app.syslog",
        function (
            $scope,
            $uibModal,
            queryFilterFactory,
            syslogService
        ) {
            //日志数据
            $scope.syslogs = [];
            $scope.levels = [
                { key: "DEBUG", value: "DEBUG" },
                { key: "INFO", value: "INFO" },
                { key: "WARN", value: "WARN" },
                { key: "ERROR", value: "ERROR" },
                { key: "FATAL", value: "FATAL" }
            ];

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
                syslogService.getAll({
                    maxResultCount: $scope.pagination.pageSize,
                    skipCount: $scope.pagination.start(),
                    startDate: $scope.filterOptions.date.startDate,
                    endDate: $scope.filterOptions.date.endDate,
                    level: $scope.filterOptions.level
                }).success(function (data) {
                    console.log(data);
                    $scope.syslogs = data.items;
                    $scope.pagination.totalCount = data.totalCount;
                });
            };

            //详情弹出框
            $scope.viewDetails = function (id) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/syslog/modal/viewDetailsModal.cshtml",
                    controller: "app.views.manage.syslog.modal.viewDetailsModal",
                    backdrop: "true",
                    resolve: {
                        deps: abp.utils.getDeps([
                            "/App/Main/views/manage/syslog/modal/viewDetailsModal.js"
                        ]),
                        syslogId: function () {
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