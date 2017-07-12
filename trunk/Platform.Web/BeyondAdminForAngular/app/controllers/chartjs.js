'use strict';
app.controller('ChartJsCtrl', [
    '$rootScope', '$scope', function ($rootScope, $scope) {
        $scope.doughnutData = [
                    {
                        value: 30,
                        color: $rootScope.settings.color.themeprimary
                    },
                    {
                        value: 50,
                        color: $rootScope.settings.color.themesecondary
                    },
                    {
                        value: 100,
                        color: $rootScope.settings.color.themethirdcolor
                    },
                    {
                        value: 40,
                        color: $rootScope.settings.color.themefourthcolor
                    },
                    {
                        value: 120,
                        color: $rootScope.settings.color.themefifthcolor
                    }

        ];
        $scope.lineChartData = {
            labels: ["", "", "", "", "", "", ""],
            datasets: [
                {
                    fillColor: "rgba(93, 178, 255,.4)",
                    strokeColor: "rgba(93, 178, 255,.7)",
                    pointColor: "rgba(93, 178, 255,.7)",
                    pointStrokeColor: "#fff",
                    data: [65, 59, 90, 81, 56, 55, 40]
                },
                {
                    fillColor: "rgba(215, 61, 50,.4)",
                    strokeColor: "rgba(215, 61, 50,.6)",
                    pointColor: "rgba(215, 61, 50,.6)",
                    pointStrokeColor: "#fff",
                    data: [28, 48, 40, 19, 96, 27, 100]
                }
            ]

        };
        $scope.pieData = [
                {
                    value: 30,
                    color: $rootScope.settings.color.themeprimary
                },
                {
                    value: 50,
                    color: $rootScope.settings.color.themesecondary
                },
                {
                    value: 100,
                    color: $rootScope.settings.color.themefourthcolor
                }

        ];
        $scope.barChartData = {
            labels: ["January", "February", "March", "April", "May", "June", "July"],
            datasets: [
                {
                    fillColor: $rootScope.settings.color.themeprimary,
                    strokeColor: $rootScope.settings.color.themeprimary,
                    data: [65, 59, 90, 81, 56, 55, 40]
                },
                {
                    fillColor: $rootScope.settings.color.themethirdcolor,
                    strokeColor: $rootScope.settings.color.themethirdcolor,
                    data: [28, 48, 40, 19, 96, 27, 100]
                }
            ]

        };
        $scope.chartData = [
                {
                    value: Math.random(),
                    color: $rootScope.settings.color.themeprimary
                },
                {
                    value: Math.random(),
                    color: $rootScope.settings.color.themesecondary
                },
                {
                    value: Math.random(),
                    color: $rootScope.settings.color.themethirdcolor
                },
                {
                    value: Math.random(),
                    color: $rootScope.settings.color.themefourthcolor
                },
                {
                    value: Math.random(),
                    color: $rootScope.settings.color.themefifthcolor
                },
                {
                    value: Math.random(),
                    color: "#ed4e2a"
                }
        ];
        $scope.radarChartData = {
            labels: ["", "", "", "", "", "", ""],
            datasets: [
                {
                    fillColor: "rgba(140,196,116,0.5)",
                    strokeColor: "rgba(140,196,116,.7)",
                    pointColor: "rgba(140,196,116,.7)",
                    pointStrokeColor: "#fff",
                    data: [65, 59, 90, 81, 56, 55, 40]
                },
                {
                    fillColor: "rgba(215,61,50,0.5)",
                    strokeColor: "rgba(215,61,50,.7)",
                    pointColor: "rgba(215,61,50,.7)",
                    pointStrokeColor: "#fff",
                    data: [28, 48, 40, 19, 96, 27, 100]
                }
            ]

        };
        new Chart(document.getElementById("doughnut").getContext("2d")).Doughnut($scope.doughnutData);
        new Chart(document.getElementById("line").getContext("2d")).Line($scope.lineChartData);
        new Chart(document.getElementById("radar").getContext("2d")).Radar($scope.radarChartData);
        new Chart(document.getElementById("polarArea").getContext("2d")).PolarArea($scope.chartData);
        new Chart(document.getElementById("bar").getContext("2d")).Bar($scope.barChartData);
        new Chart(document.getElementById("pie").getContext("2d")).Pie($scope.pieData);
    }
]);