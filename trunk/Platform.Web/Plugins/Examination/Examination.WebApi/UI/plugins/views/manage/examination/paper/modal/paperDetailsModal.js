(function () {
    //添加科目控制器
    angular.module("app").controller("app.views.manage.examination.paper.modal.addPaper", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "$uibModal",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal
            ) {

            $scope.title = "添加试卷";
            $scope.paper = {

            };

            //加载标签
            $scope.tags = [];
            $scope.loadTags = function () {
                var strs = library.points.split(","); //字符分割 
                for (var i = 0; i < strs.length ; i++) {
                    if (strs[i] !== "") {
                        $scope.tags.push({
                            text: strs[i]
                        });
                    }
                }
            };

            $scope.init = function () {
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
                    $scope.paper.subjectUnitId = selectedSubjectUnit.id;
                    $scope.paper.subjectUnit = selectedSubjectUnit;
                });
            };

            //提交事件
            $scope.save = function () {
                $scope.paper.points = "";
                $($scope.tags).each(function () {
                    $scope.paper.points = $scope.paper.points + this.text + ",";
                });

                $uibModalInstance.close($scope.paper);
            }

            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();