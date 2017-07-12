(function () {
    var controllerId = "app.views.manage.examination.paperbackup";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "$http",
        "$uibModal",
        function (
            $scope,
            appSession,
            $state,
            $http,
            $uibModal
            ) {

            $scope.init = function () {
                $scope.sum = [
                    {
                        type: "单选题",
                        point: "5",
                        selftiku: [
                            {
                                stem: "第一题题头",
                                options: [
                                    {
                                        tags: "A",
                                        content: "第一条答案"
                                    },
                                    {
                                        tags: "B",
                                        content: "第二条答案"
                                    },
                                    {
                                        tags: "C",
                                        content: "第三条答案"
                                    },
                                    {
                                        tags: "D",
                                        content: "第四条答案"
                                    }
                                ]
                            }
                        ]

                    },
                    {
                        type: "多选题",
                        point: "5",
                        selftiku: [
                            {
                                stem: "多选题第一题题头",
                                options: [
                                    {
                                        tags: "A",
                                        content: "第一条答案"
                                    },
                                    {
                                        tags: "B",
                                        content: "第二条答案"
                                    },
                                    {
                                        tags: "C",
                                        content: "第三条答案"
                                    },
                                    {
                                        tags: "D",
                                        content: "第四条答案"
                                    }
                                ]
                            }
                        ]

                    }

                ]
                $scope.shicao = {
                    type: "实操题",
                    points: "10",
                    shicaotiku: [
                        {
                            stem: "实操题题头",
                            details: [
                                {
                                    points: "2",
                                    content: "第一条评分项"
                                },
                                {
                                    points: "2",
                                    content: "第二条评分项"
                                },
                                {
                                    points: "2",
                                    content: "第三条评分项"
                                },
                                {
                                    points: "2",
                                    content: "第四条评分项"
                                }
                            ]
                        }
                    ]
                }
            }

            $scope.addSelect = function (left) {

                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/modal/addSelect.cshtml",
                    controller: "app.views.manage.examination.modal.addSelect as vm",
                    backdrop: "static",
                    size: "lg"
                });
                modalInstance.result.then(function (newdata) {
                    console.log(newdata);
                    left.selftiku.push(newdata);

                    //TODO:将newexam提交到服务端，并返回新添加的数据

                });
            }

            $scope.addSelectshicao = function (shicao) {

                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/modal/addshicao.cshtml",
                    controller: "app.views.manage.examination.modal.addshicao as vm",
                    backdrop: "static",
                    size: "lg"
                });
                modalInstance.result.then(function (newdata) {
                    console.log(newdata);
                    shicao.shicaotiku.push(newdata);

                    //TODO:将newexam提交到服务端，并返回新添加的数据

                });
            }

            $scope.editpaper = function (option) {
                var modalInstance = $uibModal.open({
                    templateUrl: "/App/Main/views/manage/examination/modal/editpaper.cshtml",
                    controller: "app.views.manage.examination.modal.editpaper as vm",
                    backdrop: "static",
                    resolve: {
                        exam: function () {
                            return angular.copy(option);
                        }
                    }
                });
                modalInstance.result.then(function (backExam) {
                    //需要把编辑之后的backExam提交到服务器，然后重新请求数据
                    console.log(backExam);
                    option.stem = backExam.stem;
                    option.options = backExam.options;

                });
            }
            $scope.delect = function (option, right) {
                for (var i = 0; i < right.selftiku.length; i++) {
                    if (right.selftiku[i] === option) {
                        right.selftiku.splice(i, 1);
                    }
                }
            }
            $scope.init();
        }
    ]);
})();