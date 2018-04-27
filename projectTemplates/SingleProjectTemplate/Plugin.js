var plugin = plugin || {};

(function (plugin) {

    plugin.$projectname$ = plugin.$projectname$ || {};
    var $projectname$LocalizationSource = abp.localization.getSource("$projectname$");
    plugin.$projectname$.localize = function () {
        return $projectname$LocalizationSource.apply(this, arguments);
    };

})(plugin);