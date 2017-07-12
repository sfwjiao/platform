(function () {
    //添加试题控制器
    angular.module("app").controller("app.views.manage.trialBalloon.myLibrary.modal.questionEdit", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        "question",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            question
            ) {

            $scope.title = "编辑试题";

            $scope.ueditorConfig = {
                initialFrameHeight: 200
            };

            $scope.question = {}
            $scope.tags = [];

            //初始化
            $scope.init = function () {
                $scope.question = question;
                $scope.loadTags();
            };

            //加载标签
            $scope.loadTags = function () {
                var strs = $scope.question.points.split(","); //字符分割 
                for (var i = 0; i < strs.length ; i++) {
                    if (strs[i] !== "") {
                        $scope.tags.push({
                            text: strs[i]
                        });
                    }
                }
            };

            //选择科目对话框
            $scope.selectSubjectUnit = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/modal/subjectUnitSelectModal.cshtml",
                    controller: "app.views.manage.trialBalloon.modal.selectSubjectUnit as vm",
                    backdrop: "static"
                });

                modalInstance.result.then(function (selectedSubjectUnit) {
                    console.log(selectedSubjectUnit);
                    $scope.question.subjectUnitId = selectedSubjectUnit.id;
                    $scope.question.subjectUnit = selectedSubjectUnit;
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
                console.log($scope.question.options);
            }

            //提交事件
            $scope.save = function () {
                $scope.question.points = "";
                $($scope.tags).each(function () {
                    $scope.question.points = $scope.question.points + this.text + ",";
                });

                console.log($scope.question);
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

