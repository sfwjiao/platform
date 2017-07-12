(function () {
    //添加考试控制器
    angular.module("app").controller("app.views.manage.examination.exam.modal.addExam", [
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

            $scope.title = "添加考试";

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

            $scope.exam = {};

            //$scope.selectSubjectUnit = function () {
            //    var modalInstance = $uibModal.open({
            //        templateUrl: "/App/Main/views/manage/trialBalloon/modal/subjectUnitSelectModal.cshtml",
            //        controller: "app.views.manage.trialBalloon.modal.selectSubjectUnit as vm",
            //        backdrop: "static"
            //    });

            //    modalInstance.result.then(function (selectedSubjectUnit) {
            //        console.log(selectedSubjectUnit);
            //        //var sun = {
            //        //    subjectUnit: selectedSubjectUnit
            //        //}
            //        $scope.exam.subjectUnits.push(selectedSubjectUnit);
            //    });
            //};

            //$scope.delectSub = function (opin) {
            //    for (i = 0; i < $scope.exam.subjectUnits.length; i++) {
            //        if ($scope.exam.subjectUnits[i] == opin) {
            //            $scope.exam.subjectUnits.splice(i, 1);
            //        }
            //    }
            //}

            $scope.init = function () {
            };

            //提交事件
            $scope.save = function () {
                console.log($scope.exam);
                $uibModalInstance.close($scope.exam);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
    //编辑考试控制器
    angular.module("app").controller("app.views.manage.examination.exam.modal.editExam", [
        "$scope",
        "$compile",
         "$state",
        "$uibModalInstance",
        "$uibModal",
        "exam",
        function (
            $scope,
            $compile,
            $state,
            $uibModalInstance,
            $uibModal,
            exam
            ) {

            $scope.title = "编辑考试";

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

            $scope.exam = {
                startDate: new Date(),
                endDate: new Date()
            };

            $scope.init = function () {
                $scope.exam = exam;
                $scope.exam.startDate = new Date(exam.startDate);
                $scope.exam.endDate = new Date(exam.endDate);
            };

            //提交事件
            $scope.save = function () {
                console.log($scope.exam);
                $uibModalInstance.close($scope.exam);
            }

            //取消事件
            $scope.cancel = function () {
                $uibModalInstance.dismiss();
            };

            $scope.init();
        }
    ]);
})();