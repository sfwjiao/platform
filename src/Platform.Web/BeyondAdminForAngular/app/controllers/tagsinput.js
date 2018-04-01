app.controller('TagsInputCtrl', function ($scope, $http) {
    $scope.tags = [
        { text: 'Amsterdam' },
        { text: 'London' },
        { text: 'Paris' },
        { text: 'Madrid' }
    ];
    $scope.loadTags = function (query) {
        return $http.get('/tags?query=' + query);
    };
});