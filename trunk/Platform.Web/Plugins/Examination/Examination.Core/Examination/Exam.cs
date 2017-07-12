using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace Examination.Examination
{
    /// <summary>
    /// 考试
    /// </summary>
    [Table("ExExams")]
    public class Exam : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 考试名称
        /// </summary>
        [Required]
        [MaxLength(MaxNameLength)]
        public virtual string Name { get; set; }

        /// <summary>
        /// 考试开始日期
        /// </summary>
        public virtual DateTime StartDate { get; set; }

        /// <summary>
        /// 考试结束日期
        /// </summary>
        public virtual DateTime EndDate { get; set; }

        /// <summary>
        /// 考试状态
        /// </summary>
        //public virtual ExaminationStatus ExaminationStatus { get; set; }
    }
}
