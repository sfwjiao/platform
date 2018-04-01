(function () {
    angular.module("app").filter("removeHtmlFilter", function () {
        return function (str) {
            var result = str.replace(/<\/?.+?>/g, "");
            return result.replace(/ /g, "");
        }
    });
})();