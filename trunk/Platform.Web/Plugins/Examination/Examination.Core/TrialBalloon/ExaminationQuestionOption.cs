using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 题目中的项
    /// </summary>
    [Table("ExExaminationQuestionOptions")]
    public class ExaminationQuestionOption : CreationAuditedEntity<long>, IMayHaveTenant
    {
        public const int MaxContentLength = 2048;
        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 所在试题
        /// </summary>
        public virtual long ExaminationQuestionId { get; set; }

        /// <summary>
        /// 所在试题
        /// </summary>
        [ForeignKey(nameof(ExaminationQuestionId))]
        public virtual ExaminationQuestion ExaminationQuestion { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [Required]
        [MaxLength(MaxContentLength)]
        public virtual string Content { get; set; }
    }
}
