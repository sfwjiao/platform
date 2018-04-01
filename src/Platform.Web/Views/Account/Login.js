var geetest_challenge = "";
var geetest_validate = "";
var geetest_seccode = "";
var CaptchaObj;

(function () {
    $(function () {
        $("#loginFrom").bootstrapValidator({
            // Only disabled elements are excluded
            // The invisible elements belonging to inactive tabs must be validated
            excluded: [':disabled'],
            submitHandler: function (validator, form, submitButton) {

                CaptchaObj.onSuccess(function () {
                    var validate = CaptchaObj.getValidate();

                    geetest_challenge = validate.geetest_challenge;
                    geetest_validate = validate.geetest_validate;
                    geetest_seccode = validate.geetest_seccode;

                    console.log(geetest_challenge);
                    console.log(geetest_validate);
                    console.log(geetest_seccode);
                    abp.ui.setBusy(
                        $('#LoginForm'),
                        abp.ajax({
                            url: abp.appPath + 'Account/Login',
                            type: 'POST',
                            data: JSON.stringify({
                                tenancyName: "Default",
                                usernameOrEmailAddress: form.context["loginid"].value,
                                password: form.context["pwd"].value,
                                rememberMe: true,
                                returnUrlHash: $('#ReturnUrlHash').val(),
                                challenge: geetest_challenge,
                                validate: geetest_validate,
                                seccode: geetest_seccode
                            })
                        })
                    );
                });

                CaptchaObj.show();
            },
            fields: {
                loginid: {
                    validators: {
                        notEmpty: {
                            message: '账号不能为空'
                        },
                        stringLength: {

                            max: 32,
                            message: '账号长度为1-32个字符'
                        }
                    }
                },
                pwd: {
                    validators: {
                        notEmpty: {
                            message: '密码不能为空'
                        },
                        stringLength: {
                            min: 6,
                            max: 32,
                            message: '密码长度为6-32个字符'
                        }
                    }
                }
            }
        });

        var handler = function (captchaObj) {
            console.log(captchaObj);
            CaptchaObj = captchaObj;
            captchaObj.appendTo("#captcha");
        };
        $.ajax({
            // 获取id，challenge，success（是否启用failback）
            url: "/account/getcaptcha",
            type: "get",
            dataType: "json", // 使用jsonp格式
            success: function (data) {
                // 使用initGeetest接口
                // 参数1：配置参数，与创建Geetest实例时接受的参数一致
                // 参数2：回调，回调的第一个参数验证码对象，之后可以使用它做appendTo之类的事件
                initGeetest({
                    gt: data.gt,
                    challenge: data.challenge,
                    product: "popup", // 产品形式
                    offline: !data.success
                }, handler);
            }
        });

    });
})();