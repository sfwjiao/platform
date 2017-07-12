(function () {
    var controllerId = "app.views.manage.examination.exam.examSubjectUnit";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "$stateParams",
        "abp.services.exam.examSubjectUnit",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            $stateParams,
            examSubjectUnitService
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

            //考试科目数据
            $scope.examSubjectUnits = [];

            //出书啊
            $scope.init = function () {
                $scope.getData();
            };

            //获取数据
            $scope.getData = function () {
                examSubjectUnitService.query({
                    pageSize: $scope.pagingOptions.pageSize,
                    start: $scope.pagingOptions.pageSize * ($scope.pagingOptions.currentPage - 1),
                    keyWord: $scope.filterOptions.filterText,
                    examId: $stateParams.id
                }).success(function (data) {
                    console.log(data);
                    $scope.examSubjectUnits = data.items;
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
                data: "examSubjectUnits",
                multiSelect: false, //是否能多选
                enableCellSelection: false, //是否能选择单元格
                enableRowSelection: false, //是否能选择行
                enableCellEdit: false, //是否能修改内容
                enablePinning: false, //是否被锁住了
                columnDefs: [
                {
                    field: "subjectUnit.displayName", //这里是数据中的属性名
                    displayName: "考试名称", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "weight", //这里是数据中的属性名
                    displayName: "权重", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "edit",
                    displayName: "操作",
                    sortable: false,//是否能排序
                    cellTemplate: '<a ng-click="edit(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-edit"></i>编辑</a><a ng-click="delete(row.entity)" class="btn btn-danger btn-xs delete margin-left-10"><i class="fa fa-trash-o"></i>删除</a>'
                }],
                enablePaging: true,//是否能翻页
                showFooter: true,//是否显示表尾
                totalServerItems: "totalServerItems",//数据的总条数 
                pagingOptions: $scope.pagingOptions, //分页部分
                filterOptions: $scope.filterOptions, //数据过滤部分
                i18n: "zh-cn"
            };

            $scope.add = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/exam/modal/examSubjectUnitDetailsModal.cshtml",
                    controller: "app.views.manage.examination.exam.modal.addExamSubjectUnit",
                    backdrop: "static",
                    resolve: {
                        examId: function () {
                            return angular.copy($stateParams.id);
                        }
                    }
                });
                modalInstance.result.then(function (backExamSubjectUnit) {
                    console.log(backExamSubjectUnit);
                    examSubjectUnitService.add(backExamSubjectUnit).success(function () {
                        $scope.getData();
                    });
                });
            };

            $scope.edit = function (examSubjectUnit) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/exam/modal/examSubjectUnitDetailsModal.cshtml",
                    controller: "app.views.manage.examination.exam.modal.editExamSubjectUnit",
                    backdrop: "static",
                    resolve: {
                        examSubjectUnit: function () {
                            return angular.copy(examSubjectUnit);
                        }
                    }
                });
                modalInstance.result.then(function (backExamSubjectUnit) {
                    console.log(backExamSubjectUnit);
                    examSubjectUnitService.edit(backExamSubjectUnit).success(function () {
                        $scope.getData();
                    });
                });
            }

            $scope.delete = function (examSubjectUnit) {
                examSubjectUnitService.delete(examSubjectUnit.id).success(function () {
                    $scope.getData();
                });
            }

            $scope.init();
        }
    ]);
})();