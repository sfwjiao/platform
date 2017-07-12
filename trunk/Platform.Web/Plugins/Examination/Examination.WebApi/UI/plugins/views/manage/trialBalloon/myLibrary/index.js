(function () {
    var controllerId = "pulgins.views.manage.trialBalloon.myLibrary";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        "abp.services.exam.examinationQuestionLibrary",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal,
            libraryService
            ) {

            $scope.librarys = [];

            $scope.init = function () {
                libraryService.queryWithOutDeletedQuestion({
                    pageSize: 0,
                    start: 0
                }).success(function (data) {
                    console.log(data);
                    $scope.librarys = data.items;
                });
            };

            $scope.addLibrary = function () {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/myLibrary/modal/libraryDetailsModal.cshtml",
                    controller: "pulgins.views.manage.trialBalloon.myLibrary.modal.addLibrary",
                    backdrop: "static"
                });

                modalInstance.result.then(function (newLibrary) {
                    libraryService.addAndGetObj(newLibrary).success(function (data) {
                        $scope.librarys.push(data);
                    });
                });
            };

            $scope.editLibrary = function (library) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/trialBalloon/myLibrary/modal/libraryDetailsModal.cshtml",
                    controller: "pulgins.views.manage.trialBalloon.myLibrary.modal.editLibrary",
                    backdrop: "static",
                    resolve: {
                        library: function () {
                            return angular.copy(library);
                        }
                    }
                });

                modalInstance.result.then(function (backLibrary) {
                    libraryService.edit(backLibrary).success(function () {
                        library.name = backLibrary.name;
                        library.points = backLibrary.points;
                    });
                });
            };

            $scope.deleteLibrary = function (library) {
                abp.message.confirm("题库中的所有题目将被一起删除，您确认要删除题库？", function (isconfirm) {
                    if (isconfirm) {
                        libraryService.delete(library.id).success(function () {
                            abp.utils.removeArrayItem(library, $scope.librarys);
                        });
                    }
                });
            };

            $scope.gotoQuestions = function (library) {
                $state.go("manage_trialBalloon_myLibrary_questions", library);
            }

            $scope.init();
        }
    ]);
})();