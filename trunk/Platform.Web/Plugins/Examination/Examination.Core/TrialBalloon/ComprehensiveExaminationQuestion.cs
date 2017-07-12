using System.Collections.Generic;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 综合题
    /// </summary>
    public sealed class ComprehensiveExaminationQuestion : ExaminationQuestion
    {
        public ComprehensiveExaminationQuestion()
        {
            QuestionType = "Comprehensive";
        }
        /// <summary>
        /// 子节点集合
        /// </summary>
        public ICollection<ExaminationQuestion> Children { get; set; }
    }
}