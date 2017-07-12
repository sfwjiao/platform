using System.Collections.Generic;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 多选题
    /// </summary>
    public sealed class MultipleChoiceExaminationQuestion : ExaminationQuestion
    {
        public MultipleChoiceExaminationQuestion()
        {
            QuestionType = "MultipleChoice";
        }
        public ICollection<MultipleChoiceExaminationQuestionOption> Options { get; set; }
    }
}

