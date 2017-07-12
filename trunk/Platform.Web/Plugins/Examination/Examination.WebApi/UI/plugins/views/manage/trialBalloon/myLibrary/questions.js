(function () {
    var controllerId = "app.views.manage.trialBalloon.myLibrary.questions";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$stateParams",
        "$http",
        "$uibModal",
        "abp.services.exam.examinationQuestion",
        function (
            $scope,
            appSession,
            $state,
            $stateParams,
            $http,
            $uibModal,
            examinationQuestionService
            ) {

            $scope.library = {};
            $scope.pagination = {
                pageSize: 1,
                pageIndex: 1,
                numPages: 0,
                totalCount: 0,
                pageNumberIndex: 1,
                pageNumberCount: 5
            };

            //初始化
            $scope.init = function () {
                if (!$stateParams.name) {
                    $state.go("manage_trialBalloon_myLibrary");
                    return;
                }
                $scope.library = $stateParams;
                console.log($stateParams);
            };

            //设置当前页码
            $scope.setPage = function (pageNo) {
                if (pageNo > $scope.pagination.numPages) pageNo = $scope.pagination.numPages;
                if (pageNo <= 0) pageNo = 1;
                $scope.pagination.pageIndex = pageNo;
            };

            //监听页码，根据页码变化请求数据
            $scope.$watch("pagination.pageIndex", function (pageIndex) {
                console.log("Page changed to: " + pageIndex);
                $scope.getQuestions(pageIndex);
            });

            //获取试题数据
            $scope.getQuestions = function () {
                //响应数据中的questionType----singleSelection：单选，multipleChoice：多选，practical：实操，comprehensive：综合
                examinationQuestionService.queryIncludeOptions({
                    pageSize: $scope.pagination.pageSize,
                    start: $scope.pagination.pageSize * ($scope.pagination.pageIndex - 1),
                    keyWord: "",
                    libraryId: $stateParams.id
                }).success(function (data) {
                    console.log(data);
                    $scope.library.questions = data;
                    $scope.pagination.totalCount = data.totalCount;
                    //$scope.$apply();
                });
            };

            //添加试题
            $scope.addQuestion = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/myLibrary/modal/questionDetailsModal.cshtml",
                    controller: "app.views.manage.trialBalloon.myLibrary.modal.addQuestion as vm",
                    backdrop: "static",
                    size: "lg",
                    resolve: {
                        library: function () {
                            return angular.copy($scope.library);
                        }
                    }
                });

                modalInstance.result.then(function (question) {
                    examinationQuestionService.addWithOptions(question).success(function () {
                        $scope.pagination.pageIndex = $scope.pagination.totalCount + 1;
                        $scope.getQuestions();
                    });
                });
            };

            //添加子试题
            $scope.addQuestionForChildren = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/myLibrary/modal/questionDetailsForChildrenModal.cshtml",
                    controller: "app.views.manage.trialBalloon.myLibrary.modal.addQuestionForChildren as vm",
                    backdrop: "static",
                    size: "lg",
                    resolve: {
                        library: function () {
                            return angular.copy($scope.library);
                        },
                        parent: function () {
                            return angular.copy($scope.library.questions.items[0]);
                        }
                    }
                });

                modalInstance.result.then(function (question) {
                    examinationQuestionService.addWithOptions(question).success(function (newQuestion) {
                        console.log(newQuestion);
                        $scope.library.questions.items[0].children.push(newQuestion);
                    });
                });
            };

            $scope.editQuestion = function (question) {
                console.log(question);
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/myLibrary/modal/questionEditModal.cshtml",
                    controller: "app.views.manage.trialBalloon.myLibrary.modal.questionEdit as vm",
                    backdrop: "static",
                    size: "lg",
                    resolve: {
                        question: function () {
                            return angular.copy(question);
                        }
                    }
                });

                modalInstance.result.then(function (newQuestion) {
                    console.log(newQuestion);
                    examinationQuestionService.editWithOptions(newQuestion).success(function () {
                        $scope.getQuestions();
                    });
                });
            }

            //删除试题
            $scope.deleteQuestion = function (question) {
                abp.message.confirm("您确认要删除当前试题？", function (isconfirm) {
                    if (isconfirm) {
                        examinationQuestionService.delete(question.id).success(function () {
                            $scope.getQuestions();
                        });
                    }
                });
            };

            $scope.init();
        }
    ]);
})();