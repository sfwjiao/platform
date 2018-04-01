var abp = abp || {};
(function ($) {
    if (!bootbox || !$) {
        return;
    }

    bootbox.setDefaults("locale", "zh_CN");

    abp.libs = abp.libs || {};
    abp.libs.bootbox = {
        config: {
            'default': {
                buttons: {
                    success: {
                        label: "确认",
                        className: "btn-blue"
                    }
                }
            },
            info: {
            },
            success: {
            },
            warn: {
            },
            error: {
            },
            confirm: {
            }
        }
    };

    var showMessage = function (type, message, title) {
        if (!title) {
            return $.Deferred(function ($dfd) {
                bootbox.alert(message, function () {
                    $dfd.resolve();
                });
            });
        } else {
            var opts = $.extend(
                {},
                abp.libs.bootbox.config.default,
                abp.libs.bootbox.config[type],
                {
                    title: title,
                    message: message
                }
            );
            return $.Deferred(function ($dfd) {
                bootbox.dialog(opts, function () {
                    $dfd.resolve();
                });
            });
        }
    };

    abp.message.info = function (message, title) {
        return showMessage('info', message, title);
    };

    abp.message.success = function (message, title) {
        return showMessage('success', message, title);
    };

    abp.message.warn = function (message, title) {
        return showMessage('warn', message, title);
    };

    abp.message.error = function (message, title) {
        return showMessage('error', message, title);
    };

    abp.message.confirm = function (message, titleOrCallback, callback) {

        if ($.isFunction(titleOrCallback)) {
            callback = titleOrCallback;
            return $.Deferred(function ($dfd) {
                bootbox.confirm(message, function (result) {
                    //console.log(result);
                    callback && callback(result);
                    $dfd.resolve();
                });
            });
        }

        var userOpts = {
            message: message
        }
        if (titleOrCallback) {
            userOpts.title = titleOrCallback;
        };

        var opts = $.extend(
            {},
            abp.libs.bootbox.config.default,
            abp.libs.bootbox.config.confirm,
            userOpts
        );

        return $.Deferred(function ($dfd) {
            bootbox.dialog(opts, function (isConfirmed) {
                callback && callback(isConfirmed);
                $dfd.resolve();
            });
        });
    };

})(jQuery);