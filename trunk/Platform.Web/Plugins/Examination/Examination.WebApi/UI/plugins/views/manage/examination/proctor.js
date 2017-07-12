(function () {
    var controllerId = "app.views.manage.watchteacher.teacherlist";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal
            ) {

            $scope.init = function () {
                $scope.teacherInformation = [
                    {
                        name: "王英杰",
                        user: "wangyingjie15090",
                        password: "3.14woaini"
                    },
                    {
                        name: "王英杰2号",
                        user: "wangyingjie15090",
                        password: "3.14woaini"
                    }, {
                        name: "王英杰3号",
                        user: "wangyingjie15090",
                        password: "3.14woaini"
                    }
                ]

            }

            //$scope.addSelect = function (left) {

            //    var modalInstance = $uibModal.open({
            //        templateUrl: "/App/Main/views/manage/examination/modal/addSelect.cshtml",
            //        controller: "app.views.manage.examination.modal.addSelect as vm",
            //        backdrop: "static",
            //        size: "lg"
            //    });
            //    modalInstance.result.then(function (newdata) {
            //        console.log(newdata);
            //        left.selftiku.push(newdata);

            //        //TODO:将newexam提交到服务端，并返回新添加的数据

            //    })
            //}

            $scope.addteacher = function () {

                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/watchteacher/modal/addteacher.cshtml",
                    controller: "app.views.manage.watchteacher.modal.addteacher  as vm",
                    backdrop: "static",

                });
                modalInstance.result.then(function (newdata) {

                    $scope.teacherInformation.push(newdata);

                    //TODO:将newexam提交到服务端，并返回新添加的数据

                })
            }

            $scope.editteacher = function (option) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/watchteacher/modal/editteacher.cshtml",
                    controller: "app.views.manage.watchteacher.modal.editteacher as vm",
                    backdrop: "static",
                    resolve: {
                        teacher: function () {
                            return angular.copy(option);
                        }
                    }
                });
                modalInstance.result.then(function (backinfo) {
                    //需要把编辑之后的backinfo提交到服务器，然后重新请求数据

                    option.name = backinfo.name;
                    option.user = backinfo.user;
                    option.password = backinfo.password;

                })
            }
            $scope.delectteacher = function (option) {
                for (var i = 0; i < $scope.teacherInformation.length; i++) {
                    if ($scope.teacherInformation[i] === option) {
                        $scope.teacherInformation.splice(i, 1);
                    }
                }
            }
            $scope.init();
        }
    ]);
})();