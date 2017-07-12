(function () {
    var controllerId = "app.views.manage.examination.exam.screenings";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$stateParams",
        "$uibModal",
        "abp.services.exam.screenings",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $stateParams,
            $uibModal,
            screeningsService
        ) {

            $scope.screeningses = [];

            //初始化
            $scope.init = function () {
                screeningsService.query({
                    pageSize: 0,
                    start: 0,
                    examId: $stateParams.id
                }).success(function (data) {
                    console.log(data);
                    $scope.screeningses = data.items;

                    for (var sindex in $scope.screeningses) {
                        $scope.screeningses[sindex].startTime = moment($scope.screeningses[sindex].startTime).format("YYYY-MM-DD HH:mm");
                    }
                });
            };

            //添加
            $scope.add = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/exam/modal/screeningsDetailsModal.cshtml",
                    controller: "app.views.manage.examination.exam.modal.addScreenings",
                    backdrop: "static"
                });
                modalInstance.result.then(function (backScreenings) {
                    console.log(backScreenings);
                    backScreenings.examId = $stateParams.id;
                    screeningsService.addAndGetObj(backScreenings).success(function (data) {
                        $scope.screeningses.push(data);
                    });
                });
            }

            //编辑
            $scope.edit = function (screenings) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/exam/modal/screeningsDetailsModal.cshtml",
                    controller: "app.views.manage.examination.exam.modal.editScreenings",
                    backdrop: "static",
                    resolve: {
                        screenings: function () {
                            return angular.copy(screenings);
                        }
                    }
                });
                modalInstance.result.then(function (backScreenings) {
                    console.log(backScreenings);
                    screeningsService.edit(backScreenings).success(function () {
                        screenings.examinationRoom = backScreenings.examinationRoom;
                        screenings.name = backScreenings.name;
                        screenings.startTime = backScreenings.startTime;
                        screenings.endTime = backScreenings.endTime;
                        screenings.examinationMode = backScreenings.examinationMode;
                    });
                });
            }
            $scope.delete = function (screenings) {
                abp.message.confirm("您确认要删除该场次？", function (isconfirm) {
                    if (isconfirm) {
                        screeningsService.delete(screenings.id).success(function () {
                            abp.utils.removeArrayItem(screenings, $scope.screeningses);
                        });
                    }
                });
            };
            $scope.gotoDetails = function (screenings) {
                $state.go("manage_examination_exam_screeningsDetails", screenings);
                console.log(screenings);
            }

            $scope.editStatus = function (screenings) {
                screeningsService.edit(screenings).success(function () {
                });
            };

            $scope.init();
        }
    ]);
}
)();
