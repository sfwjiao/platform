(function () {
    var controllerId = "app.views.manage.changePwd";
    angular.module("app").controller(controllerId, [
        "$scope", "appSession", "$state", "abp.services.app.user", function ($scope, appSession, $state, userService) {
            var vm = this;

            vm.input = {
                id: appSession.user.id
            }

            vm.submitForm = function (form) {
                if (form.$valid) {
                    userService.updatePwd(vm.input).success(function () {
                        abp.message.info("修改成功!");

                        vm.input.oldPassword = "";
                        vm.input.password = "";
                        vm.input.password2 = "";


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