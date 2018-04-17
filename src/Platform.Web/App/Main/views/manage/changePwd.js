(function () {
    var controllerId = "app.views.manage.changePwd";
    angular.module("app").controller(controllerId, [
        "$scope", "appSession", "abp.services.app.user", function ($scope, appSession, userService) {
            $scope.input = {
                id: appSession.user.id
            }

            $scope.submitForm = function (form) {
                if (form.$valid) {
                    userService.updatePwd($scope.input).success(function () {
                        abp.message.info("修改成功!");

                        $scope.input.oldPassword = "";
                        $scope.input.password = "";
                        $scope.input.password2 = "";

                        form.oldPassword.$pristine = true;
                        form.password.$pristine = true;
                        form.password2.$pristine = true;

                    });
                } else {
                    form.oldPassword.$pristine = false;
                    form.password.$pristine = false;
                    form.password2.$pristine = false;
                }
            };
        }
    ]);
})(); 