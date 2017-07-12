(function () {
    //添加试题控制器
    angular.module("app").controller("app.views.manage.examination.paper.modal.addQuestion", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "$rootScope",
        "group",
        "paper",
        "question",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            $rootScope,
            group,
            paper,
            question
            ) {

            $scope.title = "添加试题";

            $scope.viewConfig = {
                showFromLibrary: true
            };

            $scope.ueditorConfig = {
                initialFrameHeight: 200
            };

            $scope.question = {
                options: [{ isRightKey: true }, { isRightKey: false }, { isRightKey: false }, { isRightKey: false }]
            }
            $scope.tags = [];

            //初始化
            $scope.init = function () {
                $scope.question.examinationPaperId = paper.id;
                $scope.question.examinationPaperQuestionGroupId = group.id;
                if (question) {
                    $scope.question.parentId = question.id;
                    $scope.question.parent = question;
                }
                $scope.question.questionType = group.questionType;
            };

            //导入试题对话框
            $scope.fromLibrary = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/modal/questionSelectModal.cshtml",
                    controller: "app.views.manage.trialBalloon.modal.selectQuestion as vm",
                    backdrop: "static",
                    resolve: {
                        questionType: function () {
                            return angular.copy($scope.question.questionType);
                        },
                        parent: function () {
                            return angular.copy($scope.question.parent);
                        }
                    }
                });

                modalInstance.result.then(function (selectedQuestion) {
                    console.log(selectedQuestion);
                    $scope.question.questionType = selectedQuestion.questionType;
                    $scope.question.stem = selectedQuestion.stem;
                    $scope.question.options = selectedQuestion.options;
                    $scope.question.analysis = selectedQuestion.analysis;
                    //$scope.question.numberCode = selectedQuestion.numberCode;
                    //$scope.question.points = selectedQuestion.points;
                });
            };

            //控制checkbox单选
            $scope.singleChanged = function (option) {
                if (option.isRightKey) {
                    for (var i = 0; i < $scope.question.options.length; i++) {
                        $scope.question.options[i].isRightKey = false;
                    }
                    option.isRightKey = true;
                }
            }

            //删除选项
            $scope.deleteOption = function (option) {
                if ($scope.question.options.length > 1) {
                    abp.utils.removeArrayItem(option, $scope.question.options);
                    //如果删除项为单选选中项，将第一项设为选中
                    if (option.isRightKey && option.questionType === "SingleSelection") {
                        $scope.question.options[0].isRightKey = true;
                    }
                    $scope.$apply();
                }
            }

            //添加选项
            $scope.addOption = function () {
                $scope.question.options.push({});
            }

            //提交事件
            $scope.save = function () {
                $scope.question.points = "";
                $($scope.tags).each(function () {
                    $scope.question.points = $scope.question.points + this.text + ",";
                });

                $uibModalInstance.close($scope.question);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
    //添加子试题控制器
    angular.module("app").controller("app.views.manage.examination.paper.modal.addChildQuestion", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "$rootScope",
        "group",
        "paper",
        "question",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            $rootScope,
            group,
            paper,
            question
            ) {

            $scope.title = "添加子试题";

            $scope.viewConfig = {
                showQuestionType: true,
                showFromLibrary: true
            };
            $scope.ueditorConfig = {
                initialFrameHeight: 200
            };

            $scope.question = {
                options: [{ isRightKey: true }, { isRightKey: false }, { isRightKey: false }, { isRightKey: false }]
            }
            $scope.tags = [];

            //初始化
            $scope.init = function () {
                console.log(question);
                $scope.question.examinationPaperId = paper.id;
                $scope.question.examinationPaperQuestionGroupId = group.id;
                if (question) {
                    $scope.question.parentId = question.id;
                    $scope.question.parent = question;
                }
                $scope.question.questionType = group.questionType;
                console.log($scope.question);
            };

            //导入试题对话框
            $scope.fromLibrary = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/modal/questionSelectModal.cshtml",
                    controller: "app.views.manage.trialBalloon.modal.selectQuestion as vm",
                    backdrop: "static",
                    resolve: {
                        questionType: function () {
                            return angular.copy($scope.question.parent.questionType);
                        },
                        parent: function () {
                            return null;
                        }
                    }
                });

                modalInstance.result.then(function (selectedQuestion) {
                    console.log(selectedQuestion);
                    $scope.question.questionType = selectedQuestion.questionType;
                    $scope.question.stem = selectedQuestion.stem;
                    $scope.question.options = selectedQuestion.options;
                    $scope.question.analysis = selectedQuestion.analysis;
                    //$scope.question.numberCode = selectedQuestion.numberCode;
                    //$scope.question.points = selectedQuestion.points;
                });
            };

            //控制checkbox单选
            $scope.singleChanged = function (option) {
                if (option.isRightKey) {
                    for (var i = 0; i < $scope.question.options.length; i++) {
                        $scope.question.options[i].isRightKey = false;
                    }
                    option.isRightKey = true;
                }
            }

            //删除选项
            $scope.deleteOption = function (option) {
                if ($scope.question.options.length > 1) {
                    abp.utils.removeArrayItem(option, $scope.question.options);
                    //如果删除项为单选选中项，将第一项设为选中
                    if (option.isRightKey && option.questionType === "SingleSelection") {
                        $scope.question.options[0].isRightKey = true;
                    }
                    $scope.$apply();
                }
            }

            //添加选项
            $scope.addOption = function () {
                $scope.question.options.push({});
            }

            //提交事件
            $scope.save = function () {
                $scope.question.points = "";
                $($scope.tags).each(function () {
                    $scope.question.points = $scope.question.points + this.text + ",";
                });

                $uibModalInstance.close($scope.question);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
    //添加试题控制器
    angular.module("app").controller("app.views.manage.examination.paper.modal.editQuestion", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "$rootScope",
        "group",
        "paper",
        "question",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            $rootScope,
            group,
            paper,
            question
            ) {

            $scope.title = "添加试题";

            $scope.ueditorConfig = {
                initialFrameHeight: 200
            };

            $scope.question = {
                options: [{ isRightKey: true }, { isRightKey: false }, { isRightKey: false }, { isRightKey: false }]
            }
            $scope.tags = [];

            //初始化
            $scope.init = function () {
                $scope.question = question;
                $scope.question.examinationPaperId = paper.id;
                $scope.question.examinationPaperQuestionGroupId = group.id;
            };

            //控制checkbox单选
            $scope.singleChanged = function (option) {
                if (option.isRightKey) {
                    for (var i = 0; i < $scope.question.options.length; i++) {
                        $scope.question.options[i].isRightKey = false;
                    }
                    option.isRightKey = true;
                }
            }

            //删除选项
            $scope.deleteOption = function (option) {
                if ($scope.question.options.length > 1) {
                    abp.utils.removeArrayItem(option, $scope.question.options);
                    //如果删除项为单选选中项，将第一项设为选中
                    if (option.isRightKey && option.questionType === "SingleSelection") {
                        $scope.question.options[0].isRightKey = true;
                    }
                    $scope.$apply();
                }
            }

            //添加选项
            $scope.addOption = function () {
                $scope.question.options.push({});
            }

            //提交事件
            $scope.save = function () {
                $scope.question.points = "";
                $($scope.tags).each(function () {
                    $scope.question.points = $scope.question.points + this.text + ",";
                });

                $uibModalInstance.close($scope.question);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();