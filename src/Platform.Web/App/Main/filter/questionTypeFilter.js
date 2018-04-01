(function () {
    angular.module("app").filter("questionTypeFilter", function () {
        return function (fitQuestionType) {
            if (fitQuestionType === "SingleSelection") {
                return "单选题";
            } else if (fitQuestionType === "MultipleChoice") {
                return "多选题";
            } else if (fitQuestionType === "Practical") {
                return "实操题";
            } else if (fitQuestionType === "Comprehensive") {
                return "综合题";
            }
            return fitQuestionType;
        }
    });
})();