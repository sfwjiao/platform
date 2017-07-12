using System.Collections.Generic;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 单选题
    /// </summary>
    public sealed class SingleSelectionExaminationQuestion : ExaminationQuestion
    {
        public SingleSelectionExaminationQuestion()
        {
            QuestionType = "SingleSelection";
        }

        public ICollection<SingleSelectionExaminationQuestionOption> Options { get; set; }
    }
}
