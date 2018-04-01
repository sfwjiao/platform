'use strict';

app.controller('DataboxesCtrl', [
    '$rootScope', '$scope', function($rootScope, $scope) {

        //Pie Chart
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
                color: $rootScope.settings.color.themethirdcolor
            }
        ];
        new Chart(document.getElementById("pie").getContext("2d")).Pie($scope.pieData);

        //BandWidth Pie Chart 
        $scope.BandwidthPieData = [
            { data: [[1, 50]], color: '#11a9cc' },
            { data: [[1, 80]], color: '#ffce55' },
            { data: [[1, 30]], color: '#e75b8d' }
        ];
        $scope.BandwidthPieOptions = {
            series: {
                pie: {
                    innerRadius: 0.7,
                    show: true,
                    gradient: {
                        radial: true,
                        colors: [
                            { opacity: 1.0 },
                            { opacity: 1.0 },
                            { opacity: 1.0 }
                        ]
                    }
                }
            }
        };

        //Weather Pie Chart 
        $scope.WeatherPieData = [
            { data: [[1, 30]], color: '#ffce55' },
            { data: [[1, 11]], color: '#e46f61' },
            { data: [[1, 37]], color: '#ed4e2a' },
            { data: [[1, 22]], color: '#fb6e52' }
        ];
        $scope.WeatherPieOptions = {
            series: {
                pie: {
                    innerRadius: 0.80,
                    show: true,
                    gradient: {
                        radial: true,
                        colors: [
                            { opacity: 1.0 },
                            { opacity: 1.0 },
                            { opacity: 1.0 },
                            { opacity: 1.0 }
                        ]
                    }
                }
            }
        };

        //Data Transfer Bar Chart
        $scope.d1_1 = [];
        for (var i = 1; i <= 7; i += 1)
            $scope.d1_1.push([i, parseInt(Math.random() * 50)]);

        $scope.d1_2 = [];
        for (var i = 1; i <= 7; i += 1)
            $scope.d1_2.push([i, parseInt(Math.random() * 50)]);

        $scope.d1_3 = [];
        for (var i = 1; i <= 7; i += 1)
            $scope.d1_3.push([i, parseInt(Math.random() * 50)]);

        $scope.DataTransferChartData = [
            {
                label: "Windows Phone",
                data: $scope.d1_1,
                bars: {
                    show: true,
                    order: 1,
                    fillColor: { colors: [{ color: $rootScope.settings.color.themethirdcolor }, { color: $rootScope.settings.color.themethirdcolor }] }
                },
                color: $rootScope.settings.color.themethirdcolor
            },
            {
                label: "Android",
                data: $scope.d1_2,
                bars: {
                    show: true,
                    order: 2,
                    fillColor: { colors: [{ color: $rootScope.settings.color.themesecondary }, { color: $rootScope.settings.color.themesecondary }] }
                },
                color: $rootScope.settings.color.themesecondary
            },
            {
                label: "IOS",
                data: $scope.d1_3,
                bars: {
                    show: true,
                    order: 3,
                    fillColor: { colors: [{ color: $rootScope.settings.color.themeprimary }, { color: $rootScope.settings.color.themeprimary }] }
                },
                color: $rootScope.settings.color.themeprimary
            }
        ];

        $scope.DataTransferChartOptions = {
            bars: {
                barWidth: 0.2,
                lineWidth: 1,
                borderWidth: 0,
                fillColor: { colors: [{ opacity: 0.6 }, { opacity: 1 }] }
            },
            xaxis: {
                ticks: [[1, 'Sun'], [2, 'Mon'], [3, 'Tues'], [4, 'Wed'], [5, 'Thu'], [6, 'Fri'], [7, 'Sat']],
                color: '#eee'
            },
            yaxis: {
                color: '#eee'
            },
            grid: {
                hoverable: true,
                clickable: false,
                borderWidth: 0,
                aboveData: false
            },
            legend: true,
            tooltip: true,
            tooltipOpts: {
                defaultTheme: false,
                content: "<b>%s</b> : <span>%x</span> : <span>%y</span>",
            }
        };

        //Donut Chart Options
        Morris.Donut({
            element: 'donut-chart',
            data: [
              { label: 'IOS', value: 40, },
              { label: 'Win', value: 30 },
              { label: 'Android', value: 25 },
              { label: 'Java', value: 5 }
            ],
            colors: [$rootScope.settings.color.themeprimary, $rootScope.settings.color.themesecondary, $rootScope.settings.color.themethirdcolor, $rootScope.settings.color.themefourthcolor],
            formatter: function (y) { return y + "%"; }
        });
    }
]);