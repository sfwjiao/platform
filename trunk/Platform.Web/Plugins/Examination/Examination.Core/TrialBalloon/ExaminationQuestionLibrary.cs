using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 题库
    /// </summary>
    [Table("ExExaminationQuestionLibrarys")]
    public class ExaminationQuestionLibrary : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxPointsLength = 512;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        /// <summary>
        /// 考点
        /// </summary>
        [StringLength(MaxPointsLength)]
        public virtual string Points { get; set; }

        /// <summary>
        /// 试题数据集
        /// </summary>
        public virtual ICollection<ExaminationQuestion> Questions { get; set; }
    }
}