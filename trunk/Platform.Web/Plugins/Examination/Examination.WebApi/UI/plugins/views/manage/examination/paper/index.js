(function () {
    var controllerId = "app.views.manage.examination.paper";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.app.examinationPaper",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            examinationPaperService
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

            //试卷数据
            $scope.papers = [];

            $scope.init = function () {
                $scope.getData();
            }

            $scope.getData = function () {
                examinationPaperService.query({
                    pageSize: $scope.pagingOptions.pageSize,
                    start: $scope.pagingOptions.pageSize * ($scope.pagingOptions.currentPage - 1),
                    keyWord: $scope.filterOptions.filterText,
                    questionType: $scope.filterOptions.questionType,
                    libraryId: $scope.filterOptions.libraryId
                }).success(function (data) {

                    $scope.papers = data.items;
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
                data: "papers",
                //rowTemplate: '<div style="height: 100%"><div ng-style="{ \'cursor\': row.cursor }" ng-repeat="col in renderedColumns" ng-class="col.colIndex()" class="ngCell "><div class="ngVerticalBar" ng-style="{height: rowHeight}" ng-class="{ ngVerticalBarVisible: !$last }"> </div><div ng-cell></div></div></div>',
                multiSelect: false, //是否能多选
                enableCellSelection: false, //是否能选择单元格
                enableRowSelection: false, //是否能选择行
                enableCellEdit: false, //是否能修改内容
                enablePinning: false, //是否被锁住了
                columnDefs: [
                {
                    field: "name", //这里是数据中的属性名
                    displayName: "试卷名称", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "subjectUnit.displayName", //这里是数据中的属性名
                    displayName: "科目", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "answerTime", //这里是数据中的属性名
                    displayName: "答题时间", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "points", //这里是数据中的属性名
                    displayName: "标签", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "edit",
                    displayName: "操作",
                    sortable: false,//是否能排序
                    cellTemplate: '<a ng-click="details(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-newspaper-o"></i>详情</a><a ng-click="edit(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-edit"></i>编辑</a><a ng-click="delete(row.entity)" class="btn btn-danger btn-xs delete margin-left-10"><i class="fa fa-trash-o"></i>删除</a>'
                }],
                enablePaging: true,//是否能翻页
                showFooter: true,//是否显示表尾
                totalServerItems: "totalServerItems",//数据的总条数 
                pagingOptions: $scope.pagingOptions, //分页部分
                filterOptions: $scope.filterOptions, //数据过滤部分
                i18n: "zh-cn"
            };


            //添加科目
            $scope.addPaper = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/paperDetailsModal.cshtml",
                    controller: "app.views.manage.examination.paper.modal.addPaper as vm",
                    backdrop: "static"
                });

                //对话框回调
                modalInstance.result.then(function (newPaper) {
                    console.log(newPaper);
                    examinationPaperService.add(newPaper).success(function () {
                        $scope.getData();
                    });
                });
            };

            $scope.details = function (paper) {
                $state.go("manage_examination_paper_details", { id: paper.id });
            }
            //编辑试卷信息
            $scope.edit = function (paper) {

                console.log(paper);
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/editPapername.cshtml",
                    controller: "app.views.manage.examination.paper.modal.editPaper as vm",
                    backdrop: "static",
                    resolve: {
                        papers: function () {
                            console.log(1);
                            return angular.copy(paper);

                        }
                    }
                });

                modalInstance.result.then(function (backPaper) {
                    //新数据赋值到paper
                    console.log(backPaper);
                    //paper = backPaper;
                    /////// 
                    examinationPaperService.edit(backPaper)
                        .success(function () {
                            //重新获取一下数据
                            $scope.getData();
                        });
                });
            }
            //删除点击的这个Paper
            $scope.delete = function (paper) {
                console.log(paper);
                //for (i = 0; i < $scope.papers.length;i++)
                //    if ($scope.papers[i] == paper)
                //    {
                //        $scope.papers.splice(i, 1);
                //    }
                abp.message.confirm("您确认要删除该试卷？", function (isconfirm) {
                    if (isconfirm) {
                        examinationPaperService.delete(paper.id).success(function () {
                            abp.utils.removeArrayItem(paper, $scope.papers);
                        });
                    }
                });
            }

            $scope.init();
        }
    ]);
})();