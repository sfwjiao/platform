using System.ComponentModel.DataAnnotations.Schema;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 实操题的项
    /// </summary>
    [Table("ExPracticalExaminationQuestionOptions")]
    public class PracticalExaminationQuestionOption : ExaminationQuestionOption
    {
        ///// <summary>
        ///// 参考分数
        ///// </summary>
        //public double ReferenceScore { get; set; }
    }
}
