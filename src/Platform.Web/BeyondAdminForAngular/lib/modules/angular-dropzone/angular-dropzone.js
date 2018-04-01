
angular.module('dropzone', [])
    .directive('dropzone', function() {

        return function(scope, element, attrs) {

            element.dropzone({
                url: "/upload",
                maxFilesize: 100,
                paramName: "uploadfile",
                maxThumbnailFilesize: 5,
                init: function() {
                    scope.files.push({ file: 'added' }); // here works
                    this.on('success', function(file, json) {
                    });

                    this.on('addedfile', function(file) {
                        scope.$apply(function() {
                            alert(file);
                            scope.files.push({ file: 'added' });
                        });
                    });

                    this.on('drop', function(file) {
                        alert('file');
                    });

                }

            });

        };
    });