var plugin = plugin || {};

(function (plugin) {

    plugin.Cms = plugin.Cms || {};
    var CmsLocalizationSource = abp.localization.getSource("Cms");
    plugin.Cms.localize = function () {
        return CmsLocalizationSource.apply(this, arguments);
    };

})(plugin);