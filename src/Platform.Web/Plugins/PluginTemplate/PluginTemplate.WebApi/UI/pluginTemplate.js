var plugin = plugin || {};

(function () {

    plugin.pluginTemplate = plugin.pluginTemplate || {};
    var pluginTemplateLocalizationSource = abp.localization.getSource("PluginTemplate");
    plugin.pluginTemplate.localize = function () {
        return pluginTemplateLocalizationSource.apply(this, arguments);
    };

})(plugin);