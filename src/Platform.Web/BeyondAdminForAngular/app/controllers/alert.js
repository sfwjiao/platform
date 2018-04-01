app.controller('AlertDemoCtrl', function($scope) {
    $scope.alerts = [
        { type: 'danger', msg: 'Oh snap! Change a few things up and try submitting again.' },
        { type: 'success', msg: 'Well done! You successfully read this important alert message.' },
        { type: 'info', msg: 'You have 8 unread messages.' },
        { type: 'warning', msg: 'Your monthly traffic is reaching limit.' }
    ];

    $scope.addAlert = function() {
        $scope.alerts.push({ msg: 'Another alert!' });
    };

    $scope.closeAlert = function(index) {
        $scope.alerts.splice(index, 1);
    };
});