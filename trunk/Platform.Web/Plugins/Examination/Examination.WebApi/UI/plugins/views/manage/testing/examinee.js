(function () {
    var controllerId = "app.views.manage.testing.examinee";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.exam.exam",
        "$filter",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            examService,
            $filter
            ) {

            $scope.totalServerItems = 0;
            $scope.pagingOptions = {
                pageSizes: [25],
                pageSize: 25,
                currentPage: 1
            };

            $scope.filterOptions = {
                filterText: "",
                useExternalFilter: true
            };

            //考试数据
            $scope.exam = [];

            $scope.init = function () {
                //TODO:获取所有考试信息
                $scope.getData();
            };

            $scope.getData = function () {
                examService.query({
                    pageSize: $scope.pagingOptions.pageSize,
                    start: $scope.pagingOptions.pageSize * ($scope.pagingOptions.currentPage - 1),
                    keyWord: $scope.filterOptions.filterText,
                    questionType: $scope.filterOptions.questionType,
                    libraryId: $scope.filterOptions.libraryId
                }).success(function (data) {
                    console.log(data);
                    for (i = 0; i < data.items.length; i++) {
                        data.items[i].startDate = $filter('date')(data.items[i].startDate, 'yyyy-MM-dd');
                        data.items[i].endDate = $filter('date')(data.items[i].endDate, 'yyyy-MM-dd');
                    }
                    console.log(data);
                    $scope.exam = data.items;
                    //console.log($scope.examinee);
                    $scope.totalServerItems = data.totalCount;
                });
            };

            $scope.$watch("pagingOptions", function (newVal, oldVal) {
                if (newVal !== oldVal && newVal.currentPage !== oldVal.currentPage) {
                    $scope.getData();
                }
            }, true);
            $scope.$watch("filterOptions", function (newVal, oldVal) {
                if (newVal !== oldVal) {
                    $scope.getData();
                }
            }, true);

            $scope.gridOptions = {
                data: "exam",
                //rowTemplate: '<div style="height: 100%"><div ng-style="{ \'cursor\': row.cursor }" ng-repeat="col in renderedColumns" ng-class="col.colIndex()" class="ngCell "><div class="ngVerticalBar" ng-style="{height: rowHeight}" ng-class="{ ngVerticalBarVisible: !$last }"> </div><div ng-cell></div></div></div>',
                multiSelect: false, //是否能多选
                enableCellSelection: false, //是否能选择单元格
                enableRowSelection: false, //是否能选择行
                enableCellEdit: false, //是否能修改内容
                enablePinning: false, //是否被锁住了
                columnDefs: [
                {
                    field: "name", //这里是数据中的属性名
                    displayName: "考试名称", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "startDate", //这里是数据中的属性名
                    displayName: "开始日期", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "endDate", //这里是数据中的属性名
                    displayName: "结束日期", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "edit",
                    displayName: "操作",
                    sortable: false,//是否能排序
                    cellTemplate: '<a ng-click="details(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-edit"></i>考生信息</a>'
                }],
                enablePaging: true,//是否能翻页
                showFooter: true,//是否显示表尾
                totalServerItems: "totalServerItems",//数据的总条数 
                pagingOptions: $scope.pagingOptions, //分页部分
                filterOptions: $scope.filterOptions, //数据过滤部分
                i18n: "zh-cn"
            };

            $scope.details = function (examinee) {
                $state.go("manage_testing_modal_detailExaminee", { id: examinee.id });
            }

            //$scope.delete = function (paper) {
            //    console.log(paper);
            //}

            $scope.init();
        }
    ]);
})();