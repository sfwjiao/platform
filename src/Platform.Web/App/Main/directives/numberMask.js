(function () {
    angular.module("app").directive("numberMask", function () {
        return {
            restrict: "A",
            require: "ngModel",
            scope: {
                numberValue: "=ngModel"
            },
            link: function ($scope) {
                $scope.$watch("numberValue", function (newValue, oldValue) {
                    if (newValue) {
                        if (!abp.utils.isNumber(newValue)) {
                            $scope.numberValue = oldValue;
                        }
                    }
                });
            }
        };
    });
})();