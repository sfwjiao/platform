(function () {
    //选择科目控制器
    angular.module("app").controller("app.views.manage.trialBalloon.modal.selectQuestion", [
        "$scope",
        "$compile",
        "$state",
        "$http",
        "$uibModalInstance",
        "questionType",
        "parent",
        "abp.services.exam.examinationQuestion",
        "abp.services.exam.examinationQuestionLibrary",
        function (
            $scope,
            $compile,
            $state,
            $http,
            $uibModalInstance,
            questionType,
            parent,
            examinationQuestionService,
            libraryService
            ) {

            $scope.title = "选择试题";

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
                parentId: null,
                useExternalFilter: true
            };

            $scope.questions = [];
            $scope.questionTypes = [];
            $scope.librarys = [];

            $scope.init = function () {
                if (questionType) {
                    $scope.filterOptions.questionType = questionType;
                }
                if (parent) {
                    $scope.filterOptions.parentId = parent.id;
                }
                $scope.getlibrarys();
                $scope.questionTypes = eval(App.localize("QuestionTypes"));
                $scope.questionTypes.unshift({ key: "", name: "全部试题类型" });
                $scope.getData();
            };

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
                    libraryId: $scope.filterOptions.libraryId,
                    parentId: $scope.filterOptions.parentId
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
                multiSelect: false, //是否能多选
                enableCellSelection: false, //是否能选择单元格
                enableRowSelection: false, //是否能选择行
                enableCellEdit: false, //是否能修改内容
                enablePinning: false, //是否被锁住了
                columnDefs: [
                {
                    field: "examinationQuestionLibrary.name", //这里是数据中的属性名
                    displayName: "所属题库", //这里是表格的每一列的名称
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "numberCode", //这里是数据中的属性名
                    displayName: "编号", //这里是表格的每一列的名称
                    pinnable: false,
                    sortable: false //是否能排序
                }, {
                    field: "stem", //这里是数据中的属性名
                    displayName: "题干", //这里是表格的每一列的名称
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
                    displayName: "子题",
                    sortable: false,//是否能排序
                    cellTemplate: '<a ng-if="row.entity.children.length" ng-click="selectChild(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-edit"></i>子题</a>'
                }, {
                    field: "edit",
                    displayName: "选择",
                    sortable: false,//是否能排序
                    cellTemplate: '<a ng-click="select(row.entity)" class="btn btn-info btn-xs edit margin-left-10"><i class="fa fa-edit"></i>选择</a>'
                }],
                enablePaging: true,//是否能翻页
                showFooter: true,//是否显示表尾
                totalServerItems: "totalServerItems",//数据的总条数 
                pagingOptions: $scope.pagingOptions, //分页部分
                filterOptions: $scope.filterOptions, //数据过滤部分
                i18n: "zh-cn"
            };

            $scope.select = function (question) {
                $uibModalInstance.close(question);
            }

            $scope.selectChild = function (question) {
                $scope.filterOptions.questionType = "";
                $scope.filterOptions.parentId = question.id;
                $scope.getData();
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();