(function () {
    var controllerId = "app.views.manage.examination.paper.details";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "$stateParams",
        "abp.services.app.examinationPaper",
        "abp.services.app.examinationPaperQuestionGroup",
        "abp.services.app.examinationPaperQuestion",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            $stateParams,
            examinationPaperService,
            examinationPaperQuestionGroupService,
            examinationPaperQuestionService
            ) {

            //试卷数据
            $scope.paper = {};
            $scope.currentQuestion = {};

            //初始化
            $scope.init = function () {
                $scope.getData();
            }

            $scope.getData = function () {
                examinationPaperService.getWithGroupAndQuestion($stateParams.id).success(function (data) {
                    console.log(data);
                    $scope.paper = data;

                    if ($scope.paper.group.length > 0) {
                        if ($scope.paper.group[0].questions.length > 0) {
                            $scope.selectQuestion($scope.paper.group[0].questions[0]);
                        }
                    }
                });
            }

            //添加分组
            $scope.addGroup = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/groupDetailsModal.cshtml",
                    controller: "app.views.manage.examination.paper.modal.addGroup as vm",
                    backdrop: "static",
                    resolve: {
                        paper: function () {
                            return angular.copy($scope.paper);
                        }
                    }
                });
                modalInstance.result.then(function (newGroup) {
                    examinationPaperQuestionGroupService.addAndGetObj(newGroup).success(function (group) {
                        group.examinationPaper = $scope.paper;
                        group.questions = new Array();
                        $scope.paper.group.push(group);
                        console.log(group);
                    });
                });
            };

            //编辑分组
            $scope.editGroup = function (group) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/groupDetailsModal.cshtml",
                    controller: "app.views.manage.examination.paper.modal.editGroup as vm",
                    backdrop: "static",
                    resolve: {
                        paper: function () {
                            return angular.copy($scope.paper);
                        },
                        group: function () {
                            return angular.copy(group);
                        }
                    }
                });
                modalInstance.result.then(function (newGroup) {
                    console.log(newGroup);
                    examinationPaperQuestionGroupService.edit(newGroup).success(function () {
                        group.name = newGroup.name;
                        group.questionType = newGroup.questionType;
                    });
                });
            };

            //删除分组
            $scope.deleteGroup = function (group) {
                abp.message.confirm("大题中的所有题目将被一起删除，您确认要删除该大题？", function (isconfirm) {
                    if (isconfirm) {
                        examinationPaperQuestionGroupService.delete(group.id).success(function () {
                            abp.utils.removeArrayItem(group, $scope.paper.group);
                        });
                    }
                });
            };

            //选择试题
            $scope.selectQuestion = function (question) {
                //取消所有选中状态
                if ($scope.paper.group.length) {
                    for (var gindex in $scope.paper.group) {
                        if ($scope.paper.group.hasOwnProperty(gindex)) {
                            var questions = $scope.paper.group[gindex].questions;
                            if (questions.length) {
                                for (var qindex in questions) {
                                    if (questions.hasOwnProperty(qindex)) {
                                        questions[qindex].isSelected = false;
                                    }
                                }
                            }
                        }
                    }
                }
                question.isSelected = true;
                $scope.currentQuestion = question;
                console.log(question);
            }

            //添加试题
            $scope.addQuestion = function (group) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/questionDetailsModal.cshtml",
                    controller: "app.views.manage.examination.paper.modal.addQuestion",
                    backdrop: "static",
                    size: "lg",
                    resolve: {
                        group: function () {
                            return angular.copy(group);
                        },
                        paper: function () {
                            return angular.copy($scope.paper);
                        },
                        question: function () {
                            return null;
                        }
                    }
                });
                modalInstance.result.then(function (newQuestion) {
                    console.log(newQuestion);
                    examinationPaperQuestionService.addWithOptions(newQuestion).success(function (data) {
                        group.questions.push(data);
                        $scope.selectQuestion(data);
                    });
                });
            }

            //添加子题
            $scope.addChild = function (question) {
                console.log(question);
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/questionDetailsModal.cshtml",
                    controller: "app.views.manage.examination.paper.modal.addChildQuestion",
                    backdrop: "static",
                    size: "lg",
                    resolve: {
                        group: function () {
                            return angular.copy(question.examinationPaperQuestionGroup);
                        },
                        paper: function () {
                            return angular.copy($scope.paper);
                        },
                        question: function () {
                            return angular.copy(question);
                        }
                    }
                });
                modalInstance.result.then(function (newQuestion) {
                    console.log(newQuestion);
                    examinationPaperQuestionService.addWithOptions(newQuestion).success(function (data) {
                        question.children.push(data);
                    });
                });
            };

            //编辑试题
            $scope.editQuestion = function (question) {
                console.log(question);
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/paper/modal/questionDetailsModal.cshtml",
                    controller: "app.views.manage.examination.paper.modal.editQuestion",
                    backdrop: "static",
                    size: "lg",
                    resolve: {
                        group: function () {
                            return angular.copy(question.examinationPaperQuestionGroup);
                        },
                        paper: function () {
                            return angular.copy($scope.paper);
                        },
                        question: function () {
                            return angular.copy(question);
                        }
                    }
                });
                modalInstance.result.then(function (newQuestion) {
                    console.log(newQuestion);
                    examinationPaperQuestionService.editWithOptions(newQuestion).success(function (data) {
                        question.stem = newQuestion.stem;
                        question.options = newQuestion.options;
                        question.analysis = newQuestion.analysis;
                        question.score = newQuestion.score;
                    });
                });
            };

            //删除试题
            $scope.deleteQuestion = function (question) {
                abp.message.confirm("试题中的子题及数据将被一起删除，您确认要删除试题？", function (isconfirm) {
                    if (isconfirm) {
                        examinationPaperQuestionService.delete(question.id).success(function () {
                            for (var index in $scope.paper.group) {
                                if (question.examinationPaperQuestionGroup.id === $scope.paper.group[index].id) {
                                    $scope.getData();
                                }
                            }
                        });
                    }
                });
            };

            $scope.init();

        }
    ]);
})();