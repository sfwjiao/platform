var App = App || {};
(function () {

    var appLocalizationSource = abp.localization.getSource('Platform');
    App.localize = function () {
        return appLocalizationSource.apply(this, arguments);
    };

})(App);