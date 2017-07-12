using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace Examination.Examination
{
    /// <summary>
    /// 试卷题目中的项
    /// </summary>
    [Table("ExExaminationPaperOptions")]
    public class ExaminationPaperOption : CreationAuditedEntity<long>, IMayHaveTenant
    {
        public const int MaxContentLength = 2048;
        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 所属试题
        /// </summary>
        public virtual long ExaminationPaperQuestionId { get; set; }

        /// <summary>
        /// 所属试题
        /// </summary>
        public virtual ExaminationPaperQuestion ExaminationPaperQuestion { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [Required]
        [MaxLength(MaxContentLength)]
        public virtual string Content { get; set; }
    }
}
