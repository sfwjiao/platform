(function () {
    angular.module("app").factory("appSession", [
            function () {

                var session = {
                    user: null,
                    tenant: null
                };

                abp.services.app.session.getCurrentLoginInformations({ async: false }).done(function (result) {
                    session.user = result.user;
                    session.tenant = result.tenant;
                });

                return session;
            }
        ]);
})();