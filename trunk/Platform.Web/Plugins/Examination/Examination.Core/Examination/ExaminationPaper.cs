using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Examination.TrialBalloon;

namespace Examination.Examination
{
    /// <summary>
    /// 试卷
    /// </summary>
    [Table("ExExaminationPapers")]
    public class ExaminationPaper : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 试卷名称
        /// </summary>
        [Required]
        [MaxLength(MaxNameLength)]
        public virtual string Name { get; set; }

        /// <summary>
        /// 科目
        /// </summary>
        public virtual int SubjectUnitId { get; set; }

        /// <summary>
        /// 科目
        /// </summary>
        [ForeignKey(nameof(SubjectUnitId))]
        public virtual SubjectUnit SubjectUnit { get; set; }

        /// <summary>
        /// 考点
        /// </summary>
        public virtual string Points { get; set; }

        /// <summary>
        /// 答题时间（分钟）
        /// </summary>
        public virtual int AnswerTime { get; set; }
    }
}

