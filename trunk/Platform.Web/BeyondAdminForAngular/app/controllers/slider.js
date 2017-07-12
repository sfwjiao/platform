app.controller('SliderCtrl', function ($scope) {
    $scope.number = 30;
    $scope.range = {
        start: 40,
        end: 80
    };
    $scope.price = 50;
    
    $scope.getCurrency = function (value) {
        return "$" + value.toString();
    }
});