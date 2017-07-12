'use strict';
app
    // Profile Controller
    .controller('ProfileCtrl', [
        '$rootScope', '$scope', function ($rootScope, $scope) {

            $scope.visitChartData = [{
                color: $rootScope.settings.color.themeprimary,
                label: "Visits",
                data: [[3, 10], [4, 13], [5, 12], [6, 16], [7, 19], [8, 19], [9, 24], [10, 19], [11, 18], [12, 21], [13, 17],
                    [14, 14], [15, 12], [16, 14], [17, 15]]
            }];
            $scope.visitChartOptions = {
                series: {
                    lines: {
                        show: true,
                        fill: true,
                        fillColor: { colors: [{ opacity: 0.2 }, { opacity: 0 }] }
                    },
                    points: {
                        show: true
                    }
                },
                legend: {
                    show: false
                },
                xaxis: {
                    tickDecimals: 0,
                    tickLength: 0,
                    color: '#ccc'
                },
                yaxis: {
                    min: 0,
                    tickLength: 0,
                    color: '#ccc'
                },
                grid: {
                    hoverable: true,
                    clickable: false,
                    borderWidth: 0,
                    aboveData: false,
                    color: '#fbfbfb'

                },
                tooltip: true
            };
        }
    ]);