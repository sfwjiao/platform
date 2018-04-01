app.controller('MiniColorsCtrl', function ($scope) {
    $scope.color = {
        hue: '#00AA00',
        brightness: '#FFFFFF',
        saturation: '#AAAAAA',
        wheel: '#336688',
        textfield: '#BBBBBB',
        hidden: '#121212',
        inline: '#555555',
        topleft: '#444444',
        topright: '#777777',
        bottomleft: '#888888',
        bottomright: '#191919',
        opacity: '#aabbaa',
        lettercase: '#EEEEAA',
        random: '#000000'
        // formvalidation: '#EEEEAA'
    };
    //Objects for control types:
    //object for brightness
    $scope.brightnesssettings = {
        control: 'brightness'
    };

    //object for saturation
    $scope.saturationsettings = {
        control: 'saturation'
    };

    //object for wheel
    $scope.wheelsettings = {
        control: 'wheel'
    };


    //objects for input modes
    //inline textfield
    $scope.inlinesettings = {
        inline: true
    };

    //objects for positions
    $scope.topleftsettings = {
        position: 'top left'
    };

    $scope.toprightsettings = {
        position: 'top right'
    };

    $scope.bottomrightsettings = {
        position: 'bottom right'
    };


    //objects for more
    $scope.opacitysettings = {
        opacity: true
    };

    $scope.lettercasesettings = {
        letterCase: 'uppercase'
    };

    $scope.randomColor = function () {
        $scope.color.random = getRandomColor();
    };

    var getRandomColor = function () {
        var letters = '0123456789ABCDEF'.split('');
        var color = '#';
        for (var i = 0; i < 6; i++) {
            color += letters[Math.round(Math.random() * 15)];
        }
        return color;
    };

});