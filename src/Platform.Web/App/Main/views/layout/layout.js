(function () {
    var controllerId = "app.views.layout";
    angular.module("app").controller(controllerId, [
        "$rootScope",
        "$scope",
        "$localStorage",
        "$state",
        "$timeout",
        "appSession",
        function (
            $rootScope,
            $scope,
            $localStorage,
            $state,
            $timeout,
            appSession
            ) {

            $scope.languages = abp.localization.languages;
            $scope.currentLanguage = abp.localization.currentLanguage;

            $scope.menu = abp.nav.menus.MainMenu;

            $scope.currentMenuName = $state.current.menu;

            $scope.currentUser = appSession.user;

            $rootScope.$on("$stateChangeSuccess", function (event, toState, toParams, fromState, fromParams) {
                $scope.currentMenuName = toState.menu;
            });

            $rootScope.settings = {
                skin: "",
                color: {
                    themeprimary: "#2dc3e8",
                    themesecondary: "#fb6e52",
                    themethirdcolor: "#ffce55",
                    themefourthcolor: "#a0d468",
                    themefifthcolor: "#e75b8d"
                },
                fixed: {
                    navbar: false,
                    sidebar: false,
                    breadcrumbs: false,
                    header: false
                }
            };


            if (angular.isDefined($localStorage.settings))
                $rootScope.settings = $localStorage.settings;
            else
                $localStorage.settings = $rootScope.settings;

            $rootScope.$watch("settings", function () {
                if ($rootScope.settings.fixed.header) {
                    $rootScope.settings.fixed.navbar = true;
                    $rootScope.settings.fixed.sidebar = true;
                    $rootScope.settings.fixed.breadcrumbs = true;
                }
                if ($rootScope.settings.fixed.breadcrumbs) {
                    $rootScope.settings.fixed.navbar = true;
                    $rootScope.settings.fixed.sidebar = true;
                }
                if ($rootScope.settings.fixed.sidebar) {
                    $rootScope.settings.fixed.navbar = true;

                    if (!$(".page-sidebar").hasClass("menu-compact")) {
                        $(".sidebar-menu").slimscroll({
                            position: "left",
                            size: "3px",
                            color: $rootScope.settings.color.themeprimary,
                            height: $(window).height() - 90
                        });
                    }
                } else {
                    if ($(".sidebar-menu").closest("div").hasClass("slimScrollDiv")) {
                        $(".sidebar-menu").slimScroll({ destroy: true });
                        $(".sidebar-menu").attr("style", "");
                    }
                }

                $localStorage.settings = $rootScope.settings;
            }, true);
        }]);
})();