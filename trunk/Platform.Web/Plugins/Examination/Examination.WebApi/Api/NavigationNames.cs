namespace Examination.Api
{
    public class NavigationNames
    {
        public const string TrialBalloon = "TrialBalloon";
        public const string TrialBalloonSubjectUnit = "TrialBalloon_SubjectUnit";
        public const string TrialBalloonMyLibrary = "TrialBalloon_MyLibrary";
        public const string TrialBalloonQuestion = "TrialBalloon_Question";
        public const string TrialBalloonPoints = "TrialBalloon_Points";

        public const string Examination = "Examination";
        public const string ExaminationExam = "ExaminationExam";
        public const string ExaminationPaper = "ExaminationPaper";

        public static class Propertys
        {
            public const string Order = "Order";
            public const string IsVisible = "IsVisible";
            public const string IsEnabled = "IsEnabled";
            public const string RequiredPermissionName = "RequiredPermissionName";
            public const string Icon = "Icon";
            public const string Url = "Url";
        }

        public static string GetName(string nav, string property)
            => $"{ExaminationConsts.LocalizationSourceName}.{nav}.{property}";
    }
}
