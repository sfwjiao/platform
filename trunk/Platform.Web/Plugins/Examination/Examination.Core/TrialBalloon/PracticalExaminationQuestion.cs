using System.Collections.Generic;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 实操题
    /// </summary>
    public sealed class PracticalExaminationQuestion : ExaminationQuestion
    {
        public PracticalExaminationQuestion()
        {
            QuestionType = "Practical";
        }
        public ICollection<PracticalExaminationQuestionOption> Options { get; set; }
    }
}