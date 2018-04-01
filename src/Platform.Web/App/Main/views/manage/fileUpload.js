(function () {
    var controllerId = "app.views.manage.fileUpload";
    angular.module("app").controller(controllerId, [
        "$scope",
        "appSession",
        "$state",
        "Upload",
        function (
            $scope,
            appSession,
            $state,
            Upload
        ) {

            $scope.uploadImg = '';
            //提交
            $scope.submit = function () {
                console.log($scope.file);

                //$scope.upload($scope.file);
            };
            $scope.upload = function (file) {
                $scope.fileInfo = file;
                Upload.upload({
                    //服务端接收
                    url: 'Ashx/UploadFile.ashx',
                    //上传的同时带的参数
                    data: { 'username': $scope.username },
                    file: file
                }).progress(function (evt) {
                    //进度条
                    var progressPercentage = parseInt(100.0 * evt.loaded / evt.total);
                    console.log('progess:' + progressPercentage + '%' + evt.config.file.name);
                }).success(function (data, status, headers, config) {
                    //上传成功
                    console.log('file ' + config.file.name + 'uploaded. Response: ' + data);
                    $scope.uploadImg = data;
                }).error(function (data, status, headers, config) {
                    //上传失败
                    console.log('error status: ' + status);
                });
            }
        }
    ]);
})();