(function () {
    var controllerId = "app.views.manage.examination.exam.examsum";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.exam.exam",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            examsService
        ) {
            $scope.exams = [];


            $scope.init = function () {
                examsService.queryIncludeSubjectUnits({
                    pageSize: 0,
                    start: 0
                }).success(function (data) {
                    $scope.exams = data.items;
                });

            };

            $scope.add = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/exam/modal/examDetailsModal.cshtml",
                    controller: "app.views.manage.examination.exam.modal.addExam",
                    backdrop: "static"
                });

                modalInstance.result.then(function (newexam) {
                    console.log(newexam);
                    var newexams = {
                        endDate: newexam.endDate,
                        startDate: newexam.startDate,
                        name: newexam.name,
                        examinationStatus: newexam.examinationStatus,
                        subjectUnits: newexam.subjectUnits
                    };
                    console.log(newexam.subjectUnits);

                    //TODO:将newexam提交到服务端，并返回新添加的数据
                    console.log(examsService);
                    //examsService.addWithSubject(newexams)
                    //    .success(function (data) {
                    //        console.log(data);
                    //        $scope.examsum.push(data);
                    //    });
                    examsService.addAndGetObj(newexam).success(function (data) {
                        console.log(data);
                        $scope.exams.push(data);
                    });

                });
            }

            $scope.delete = function (exam) {
                abp.message.confirm("考试中的所有内容将被一起删除，您确认要删除该考试？", function (isconfirm) {
                    if (isconfirm) {
                        examsService.delete(exam.id).success(function () {
                            abp.utils.removeArrayItem(exam, $scope.exams);
                        });
                    }
                });
            };

            $scope.edit = function (exam) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/exam/modal/examDetailsModal.cshtml",
                    controller: "app.views.manage.examination.exam.modal.editExam",
                    backdrop: "static",
                    resolve: {
                        exam: function () {
                            return angular.copy(exam);
                        }
                    }
                });
                modalInstance.result.then(function (backExam) {
                    //examsService.editWithSubjectUnit(backExam).success(function () {
                    //    exam.name = backExam.name;
                    //    exam.startDate = backExam.startDate;
                    //    exam.endDate = backExam.endDate;
                    //    exam.subjectUnits = backExam.subjectUnits;
                    //});
                    examsService.edit(backExam).success(function () {
                        exam.name = backExam.name;
                        exam.startDate = backExam.startDate;
                        exam.endDate = backExam.endDate;
                    });
                });
            }

            $scope.editExamSubjectUnit = function (exam) {
                $state.go("manage_examination_exam_examSubjectUnit", { id: exam.id });
            }

            $scope.editScreeningses = function (exam) {
                $state.go("manage_examination_exam_screenings", { id: exam.id });
            }

            $scope.editStatus = function (exam) {
                examsService.edit(exam).success(function () {
                });
            };

            $scope.init();
        }
    ]);
}
)();