using System.ComponentModel.DataAnnotations.Schema;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 多选题的项
    /// </summary>
    [Table("ExMultipleChoiceExaminationQuestionOptions")]
    public class MultipleChoiceExaminationQuestionOption : ExaminationQuestionOption
    {
        /// <summary>
        /// 是否正确
        /// </summary>
        public virtual bool IsRight { get; set; }
    }
}