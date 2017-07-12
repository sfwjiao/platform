using System.Collections.Generic;

namespace Examination.Examination
{
    /// <summary>
    /// 综合题
    /// </summary>
    public sealed class ComprehensiveExaminationPaperQuestion : ExaminationPaperQuestion
    {
        public ComprehensiveExaminationPaperQuestion()
        {
            QuestionType = "Comprehensive";
        }
        /// <summary>
        /// 子节点集合
        /// </summary>
        public ICollection<ExaminationPaperQuestion> Children { get; set; }
    }
}