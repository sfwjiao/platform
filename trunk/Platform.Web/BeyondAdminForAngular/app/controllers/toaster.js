app.controller('ToasterCtrl', ['$scope', 'toaster', function ($scope, toaster) {
    $scope.toaster = {
        type: 'success',
        title: 'Title',
        text: 'Body'
    };
    $scope.pop = function () {
        toaster.pop($scope.toaster.type, $scope.toaster.title, $scope.toaster.text);
    };
}]);