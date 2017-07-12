(function () {
    //添加题库控制器
    angular.module("app").controller("app.views.manage.examination.exam.modal.addScreenings", [
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

            $scope.title = "添加场次";

            $scope.datepickerForStart = {
                format: "yyyy-MM-dd",
                opened: false,
                dateOptions: {
                    formatYear: "yy",
                    startingDay: 1
                },
                open: function ($event) {
                    $event.preventDefault();
                    $event.stopPropagation();

                    $scope.datepickerForStart.opened = true;
                }
            };
            $scope.datepickerForEnd = {
                format: "yyyy-MM-dd",
                opened: false,
                dateOptions: {
                    formatYear: "yy",
                    startingDay: 1
                },
                open: function ($event) {
                    $event.preventDefault();
                    $event.stopPropagation();

                    $scope.datepickerForEnd.opened = true;
                }
            };

            $scope.screenings = {};
            $scope.questionTypes = [];
            $scope.templateData = {}

            $scope.init = function () {
                $scope.examinationModes = eval(App.localize("ExaminationModes"));
            };

            //提交事件
            $scope.save = function () {
                $scope.screenings.startTime = new Date($scope.templateData.startDate).format("yyyy-MM-dd") + " " + $scope.templateData.startTime;
                $scope.screenings.endTime = new Date($scope.templateData.endDate).format("yyyy-MM-dd") + " " + $scope.templateData.endTime;
                $uibModalInstance.close($scope.screenings);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
    //添加题库控制器
    angular.module("app").controller("app.views.manage.examination.exam.modal.editScreenings", [
        "$scope",
        "$compile",
        "$state",
        "$uibModalInstance",
        "screenings",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            screenings
            ) {

            $scope.title = "编辑场次";

            $scope.datepickerForStart = {
                format: "yyyy-MM-dd",
                opened: false,
                dateOptions: {
                    formatYear: "yy",
                    startingDay: 1
                },
                open: function ($event) {
                    $event.preventDefault();
                    $event.stopPropagation();

                    $scope.datepickerForStart.opened = true;
                }
            };
            $scope.datepickerForEnd = {
                format: "yyyy-MM-dd",
                opened: false,
                dateOptions: {
                    formatYear: "yy",
                    startingDay: 1
                },
                open: function ($event) {
                    $event.preventDefault();
                    $event.stopPropagation();

                    $scope.datepickerForEnd.opened = true;
                }
            };

            $scope.screenings = {};
            $scope.questionTypes = [];
            $scope.templateData = {}

            $scope.init = function () {
                $scope.examinationModes = eval(App.localize("ExaminationModes"));
                $scope.screenings = screenings;
                console.log(screenings);
                $scope.templateData.startDate = new Date(screenings.startTime).format("yyyy-MM-dd");
                $scope.templateData.startTime = new Date(screenings.startTime).format("hh:mm");
                $scope.templateData.endDate = new Date(screenings.endTime).format("yyyy-MM-dd");
                $scope.templateData.endTime = new Date(screenings.endTime).format("hh:mm");

                console.log($scope.templateData.startTime);
            };

            //提交事件
            $scope.save = function () {
                $scope.screenings.startTime = new Date($scope.templateData.startDate).format("yyyy-MM-dd") + " " + $scope.templateData.startTime;
                $scope.screenings.endTime = new Date($scope.templateData.endDate).format("yyyy-MM-dd") + " " + $scope.templateData.endTime;
                $uibModalInstance.close($scope.screenings);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();