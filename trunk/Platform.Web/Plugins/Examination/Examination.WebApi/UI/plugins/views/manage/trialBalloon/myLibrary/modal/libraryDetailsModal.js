(function () {
    //添加题库控制器
    angular.module("app").controller("pulgins.views.manage.trialBalloon.myLibrary.modal.addLibrary", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance
            ) {

            $scope.title = "添加题库";
            $scope.library = {};
            $scope.tags = [];

            $scope.init = function () {
            };

            $scope.loadTags = function () {
            };

            //提交事件
            $scope.save = function () {
                $scope.library.points = "";

                $($scope.tags).each(function () {
                    $scope.library.points = $scope.library.points + this.text + ",";
                    console.log(this.text);

                });

                $uibModalInstance.close($scope.library);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);

    //修改题库控制器
    angular.module("app").controller("pulgins.views.manage.trialBalloon.myLibrary.modal.editLibrary", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "library",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            library
            ) {

            $scope.title = "修改题库";
            $scope.library = library;
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
                $scope.loadTags();

            };

            //提交事件
            $scope.save = function () {
                $scope.library.points = "";
                $($scope.tags).each(function () {
                    $scope.library.points = $scope.library.points + this.text + ",";
                });

                $uibModalInstance.close($scope.library);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();