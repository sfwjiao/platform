var plugin = plugin || {};

(function () {

    plugin.examination = plugin.examination || {};
    var examinationLocalizationSource = abp.localization.getSource('Examination');
    plugin.examination.localize = function () {
        return examinationLocalizationSource.apply(this, arguments);
    };

})(plugin);