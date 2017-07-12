(function () {
    var controllerId = "app.views.manage.testing.model.detailExaminee";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$stateParams",
        "$uibModal",
        "abp.services.exam.examinee",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $stateParams,
            $uibModal,
            examineeService
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

            //学生数据
            $scope.examinee = [];

            $scope.init = function () {
                //TODO:获取所有考生信息
                $scope.getData();
            };

            $scope.getData = function () {
                if (!$stateParams.id) {
                    $state.go("manage_testing_examinee");
                    return;
                }
                //todo:传过来的考试信息
                console.log($stateParams);

                examineeService.query({
                    pageSize: $scope.pagingOptions.pageSize,
                    start: $scope.pagingOptions.pageSize * ($scope.pagingOptions.currentPage - 1),
                    keyWord: $scope.filterOptions.filterText,
                    //questionType: $scope.filterOptions.questionType,
                    //libraryId: $scope.filterOptions.libraryId,
                    examId: $stateParams.id
                }).success(function (data) {
                    console.log(data);
                    $scope.examinee = data.items;
                    $scope.totalServerItems = data.totalCount;
                });
            };

            $scope.gridOptions = {
                data: "examinee",
                //rowTemplate: '<div style="height: 100%"><div ng-style="{ \'cursor\': row.cursor }" ng-repeat="col in renderedColumns" ng-class="col.colIndex()" class="ngCell "><div class="ngVerticalBar" ng-style="{height: rowHeight}" ng-class="{ ngVerticalBarVisible: !$last }"> </div><div ng-cell></div></div></div>',
                multiSelect: false, //是否能多选
                enableCellSelection: false, //是否能选择单元格
                enableRowSelection: false, //是否能选择行
                enableCellEdit: false, //是否能修改内容
                enablePinning: false, //是否被锁住了
                columnDefs: [
                {
                    field: "department", //这里是数据中的属性名
                    displayName: "学院", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "className", //这里是数据中的属性名
                    displayName: "班级", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "number", //这里是数据中的属性名
                    displayName: "学号", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "name", //这里是数据中的属性名
                    displayName: "姓名", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "gender", //这里是数据中的属性名
                    displayName: "性别", //这里是表格的每一列的名称
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
            $scope.addExaminee = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/testing/modal/addExaminee.cshtml",
                    controller: "app.views.manage.testing.modal.addExaminee as vm",
                    backdrop: "static"
                });

                modalInstance.result.then(function (examinee) {
                    console.log(examinee);
                    examinee.examId = $stateParams.id;
                    examineeService.addAndGetObj(examinee).success(function (data) {
                        $scope.examinee.push(data);
                    });

                });
            }

            $scope.import = function () {
                $.ajax({
                    type: "Post",
                    url: "/ImportExcel/Import",
                    data: { examId: $stateParams.id },
                    success: function (data) {
                        alert("success");
                    },
                    error: function (msg) {
                        alert(msg);
                    }
                });
            }



            $scope.edit = function (examinee) {
                console.log(examinee);
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/testing/modal/editExaminee.cshtml",
                    controller: "app.views.manage.testing.modal.editExaminee as vm",
                    backdrop: "static",
                    resolve: {
                        library: function () {
                            return angular.copy(examinee);
                        }
                    }
                });

                modalInstance.result.then(function (backexaminee) {
                    examineeService.edit(backexaminee).success(function () {
                        examinee = backexaminee;
                        $scope.getData();

                    });
                });
            }

            $scope.delete = function (option) {
                console.log(option);
                abp.message.confirm("您确认要删除该考生？", function (isconfirm) {
                    if (isconfirm) {
                        examineeService.delete(option.id).success(function () {
                            abp.utils.removeArrayItem(option, $scope.examinee);
                        });
                    }
                });
            }

            $scope.init();
        }
    ]);
})();