(function () {
    angular.module("app").filter("localDate", function () {
        return function (date, formatstr) {
            return moment(date).format(formatstr);
        }
    });
})();