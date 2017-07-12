(function () {
    var controllerId = "app.views.manage.trialBalloon.question";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.exam.examinationQuestion",
        "abp.services.exam.examinationQuestionLibrary",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            examinationQuestionService,
            libraryService
            ) {

            //分页数据
            //$scope.pagination = {
            //    pageSize: 1,
            //    pageIndex: 1,
            //    numPages: 0,
            //    totalCount: 0,
            //    pageNumberIndex: 1,
            //    pageNumberCount: 5
            //};
            $scope.totalServerItems = 0;
            $scope.pagingOptions = {
                pageSizes: [25],
                pageSize: 25,
                currentPage: 1
            };

            $scope.filterOptions = {
                filterText: "",
                questionType: "",
                libraryId: -1,
                useExternalFilter: true
            };

            //试题数据
            $scope.questions = [];
            $scope.questionTypes = [];
            $scope.librarys = [];

            $scope.init = function () {
                $scope.getlibrarys();
                $scope.questionTypes = eval(App.localize("QuestionTypes"));
                $scope.questionTypes.unshift({ key: "", name: "全部试题类型" });
                $scope.getData();
            }

            $scope.getlibrarys = function () {
                libraryService.query({
                    pageSize: 0,
                    start: 0
                }).success(function (data) {
                    console.log(data);
                    $scope.librarys = data.items;
                    $scope.librarys.unshift({ id: -1, name: "全部题库" });
                });
            }

            $scope.getData = function () {
                examinationQuestionService.queryIncludeOptions({
                    pageSize: $scope.pagingOptions.pageSize,
                    start: $scope.pagingOptions.pageSize * ($scope.pagingOptions.currentPage - 1),
                    keyWord: $scope.filterOptions.filterText,
                    questionType: $scope.filterOptions.questionType,
                    libraryId: $scope.filterOptions.libraryId
                }).success(function (data) {
                    console.log(data);
                    $scope.questions = data.items;
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
                data: "questions",
                //rowTemplate: '<div style="height: 100%"><div ng-style="{ \'cursor\': row.cursor }" ng-repeat="col in renderedColumns" ng-class="col.colIndex()" class="ngCell "><div class="ngVerticalBar" ng-style="{height: rowHeight}" ng-class="{ ngVerticalBarVisible: !$last }"> </div><div ng-cell></div></div></div>',
                multiSelect: false, //是否能多选
                enableCellSelection: false, //是否能选择单元格
                enableRowSelection: false, //是否能选择行
                enableCellEdit: false, //是否能修改内容
                enablePinning: false, //是否被锁住了
                columnDefs: [
                {
                    field: "examinationQuestionLibrary.name", //这里是数据中的属性名
                    displayName: "所属题库", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "numberCode", //这里是数据中的属性名
                    displayName: "编号", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "stem", //这里是数据中的属性名
                    displayName: "题干", //这里是表格的每一列的名称
                    //width: 60,//表格的宽度
                    pinnable: false,
                    cellFilter: "removeHtmlFilter",
                    sortable: false //是否能排序
                }, {
                    field: "questionType",
                    displayName: "试题类型",
                    enableCellEdit: true,
                    cellFilter: "questionTypeFilter",
                    sortable: false //是否能排序
                }, {
                    field: "edit",
                    displayName: "操作",
                    sortable: false,//是否能排序
                    cellTemplate: '<a ng-click="details(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-edit"></i>编辑</a><a ng-click="delete(row.entity)" class="btn btn-danger btn-xs delete margin-left-10"><i class="fa fa-trash-o"></i>删除</a>'
                }],
                enablePaging: true,//是否能翻页
                showFooter: true,//是否显示表尾
                totalServerItems: "totalServerItems",//数据的总条数 
                pagingOptions: $scope.pagingOptions, //分页部分
                filterOptions: $scope.filterOptions, //数据过滤部分
                i18n: "zh-cn"
            };

            $scope.details = function (question) {
                console.log(question);
            }

            $scope.delete = function (question) {
                console.log(question);
            }

            $scope.init();
        }
    ]);
})();