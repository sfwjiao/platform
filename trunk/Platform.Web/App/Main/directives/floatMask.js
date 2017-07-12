(function () {
    angular.module("app").directive("floatMask", function () {
        return {
            restrict: "A",
            require: "ngModel",
            scope: {
                floatValue: "=ngModel"
            },
            link: function ($scope) {
                $scope.$watch("floatValue", function (newValue, oldValue) {
                    if (newValue) {
                        if (!abp.utils.isFloat(newValue)) {
                            $scope.floatValue = oldValue;
                        }
                    }
                });
            }
        };
    });
})();