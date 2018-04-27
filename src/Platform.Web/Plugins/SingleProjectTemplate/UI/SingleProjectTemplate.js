var plugin = plugin || {};

(function (plugin) {

    plugin.singleProjectTemplate = plugin.singleProjectTemplate || {};
    var singleProjectTemplateLocalizationSource = abp.localization.getSource("SingleProjectTemplate");
    plugin.singleProjectTemplate.localize = function () {
        return singleProjectTemplateLocalizationSource.apply(this, arguments);
    };

})(plugin);