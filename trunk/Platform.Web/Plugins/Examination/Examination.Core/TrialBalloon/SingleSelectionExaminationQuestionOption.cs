using System.ComponentModel.DataAnnotations.Schema;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 单选题的项
    /// </summary>
    [Table("ExSingleSelectionExaminationQuestionOptions")]
    public class SingleSelectionExaminationQuestionOption : ExaminationQuestionOption
    {
        /// <summary>
        /// 是否正确
        /// </summary>
        public virtual bool IsRightKey { get; set; }
    }
}
