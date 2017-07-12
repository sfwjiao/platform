using System.Collections.Generic;
using Abp.Configuration;
using Abp.Localization;

namespace Examination.Api
{
    public class ExaminationSettingProvider: SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                //题库管理
                CreateNavigationSetting(NavigationNames.TrialBalloon, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.TrialBalloon, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloon, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloon, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.TrialBalloon, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.TrialBalloon, NavigationNames.Propertys.Url, null),
                
                //科目管理
                CreateNavigationSetting(NavigationNames.TrialBalloonSubjectUnit, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.TrialBalloonSubjectUnit, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonSubjectUnit, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonSubjectUnit, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.TrialBalloonSubjectUnit, NavigationNames.Propertys.Icon, "fa fa-navicon"),
                CreateNavigationSetting(NavigationNames.TrialBalloonSubjectUnit, NavigationNames.Propertys.Url, "#/exam_subjectUnit"),

                //知识点管理
                CreateNavigationSetting(NavigationNames.TrialBalloonPoints, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.TrialBalloonPoints, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonPoints, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonPoints, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.TrialBalloonPoints, NavigationNames.Propertys.Icon, "fa fa-navicon"),
                CreateNavigationSetting(NavigationNames.TrialBalloonPoints, NavigationNames.Propertys.Url, "#/exam_points"),

                //我的题库
                CreateNavigationSetting(NavigationNames.TrialBalloonMyLibrary, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.TrialBalloonMyLibrary, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonMyLibrary, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonMyLibrary, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.TrialBalloonMyLibrary, NavigationNames.Propertys.Icon, "fa fa-file-archive-o"),
                CreateNavigationSetting(NavigationNames.TrialBalloonMyLibrary, NavigationNames.Propertys.Url, "#/exam_myLibrary"),

                //试题管理
                CreateNavigationSetting(NavigationNames.TrialBalloonQuestion, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.TrialBalloonQuestion, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonQuestion, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.TrialBalloonQuestion, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.TrialBalloonQuestion, NavigationNames.Propertys.Icon, "fa fa-question-circle"),
                CreateNavigationSetting(NavigationNames.TrialBalloonQuestion, NavigationNames.Propertys.Url, "#/exam_question"),

                //考试管理
                CreateNavigationSetting(NavigationNames.Examination, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.Examination, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.Examination, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.Examination, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.Examination, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.Examination, NavigationNames.Propertys.Url, null),

                //我的考试
                CreateNavigationSetting(NavigationNames.ExaminationExam, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.ExaminationExam, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.ExaminationExam, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.ExaminationExam, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.ExaminationExam, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.ExaminationExam, NavigationNames.Propertys.Url, null),

                //试卷管理
                CreateNavigationSetting(NavigationNames.ExaminationPaper, NavigationNames.Propertys.Order, "0"),
                CreateNavigationSetting(NavigationNames.ExaminationPaper, NavigationNames.Propertys.IsVisible, "true"),
                CreateNavigationSetting(NavigationNames.ExaminationPaper, NavigationNames.Propertys.IsEnabled, "true"),
                CreateNavigationSetting(NavigationNames.ExaminationPaper, NavigationNames.Propertys.RequiredPermissionName, null),
                CreateNavigationSetting(NavigationNames.ExaminationPaper, NavigationNames.Propertys.Icon, "fa fa-gear"),
                CreateNavigationSetting(NavigationNames.ExaminationPaper, NavigationNames.Propertys.Url, null),

            };
        }

        private static SettingDefinition CreateNavigationSetting(string nav, string property, string value)
        {
            return new SettingDefinition(
                NavigationNames.GetName(nav, property),
                value,
                L($"{nav}{property}"),
                scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                isVisibleToClients: true);
        }

        private static LocalizableString L(string name)
        {
            return new LocalizableString(name, ExaminationConsts.LocalizationSourceName);
        }
    }
}
