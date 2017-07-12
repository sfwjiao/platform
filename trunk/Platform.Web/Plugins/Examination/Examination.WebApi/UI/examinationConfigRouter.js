(function () {
    "use strict";

    //Configuration for Angular UI routing.
    angular.module("app").config([
	    "$stateProvider", "$urlRouterProvider",
	    function ($stateProvider, $urlRouterProvider) {

	        $stateProvider
	            .state("exam_subjectUnit", {
	                url: "/exam_subjectUnit",
	                templateUrl: "/App/Main/plugins/views/manage/trialBalloon/subjectUnit.cshtml",
	                menu: "exam_subjectUnit",
	                ncyBreadcrumb: {
	                    label: plugin.examination.localize("TrialBalloon_SubjectUnit"),
	                    description: plugin.examination.localize("TrialBalloon_SubjectUnitDescription")
	                },
	                resolve: {
	                    deps: [
							"$ocLazyLoad",
							function ($ocLazyLoad) {
							    return $ocLazyLoad.load({
							        serie: true,
							        files: [
										"/App/Main/plugins/views/manage/trialBalloon/subjectUnit.js",
										"/App/Main/plugins/views/manage/trialBalloon/modal/subjectUnitDetailsModal.js"
							        ]
							    });
							}
	                    ]
	                }
	            })

            .state("exam_myLibrary", {
                url: "/exam_myLibrary",
                templateUrl: "/App/Main/plugins/views/manage/trialBalloon/myLibrary/index.cshtml",
                menu: "exam_myLibrary",
                ncyBreadcrumb: {
                    label: plugin.examination.localize("TrialBalloon_MyLibrary"),
                    description: plugin.examination.localize("TrialBalloon_MyLibraryDescription")
                },
                resolve: {
                    deps: [
                        "$ocLazyLoad",
                        function ($ocLazyLoad) {
                            return $ocLazyLoad.load({
                                serie: true,
                                files: [
                                    "/App/Main/plugins/views/manage/trialBalloon/myLibrary/index.js",
                                    "/App/Main/plugins/views/manage/trialBalloon/myLibrary/modal/libraryDetailsModal.js"
                                ]
                            });
                        }
                    ]
                }
            })

            .state("exam_question", {
                url: "/exam_question",
                templateUrl: "/App/Main/plugins/views/manage/trialBalloon/question.cshtml",
                menu: "exam_question",
                ncyBreadcrumb: {
                    label: plugin.examination.localize("TrialBalloon_Question"),
                    description: plugin.examination.localize("TrialBalloon_QuestionDescription")
                },
                resolve: {
                    deps: [
                        "$ocLazyLoad",
                        function ($ocLazyLoad) {
                            return $ocLazyLoad.load({
                                serie: true,
                                files: [
                                    "/App/Main/plugins/views/manage/trialBalloon/question.js"
                                ]
                            });
                        }
                    ]
                }
            })
            .state("exam_points", {
                url: "/exam_points",
                templateUrl: "/App/Main/plugins/views/manage/trialBalloon/points/points.cshtml",
                menu: "exam_points",
                ncyBreadcrumb: {
                    label: plugin.examination.localize("TrialBalloon_Points"),
                    description: plugin.examination.localize("TrialBalloon_PointsDescription")
                },
                resolve: {
                    deps: [
                        "$ocLazyLoad",
                        function ($ocLazyLoad) {
                            return $ocLazyLoad.load({
                                serie: true,
                                files: [
                                    "/App/Main/plugins/views/manage/trialBalloon/points/points.js",
                                    "/App/Main/plugins/views/manage/trialBalloon/points/modal/pointsDetailsModal.js"
                                ]
                            });
                        }
                    ]
                }
            })

	        ;
	    }



    ]);
})();