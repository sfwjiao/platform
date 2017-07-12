using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace Examination.Examination
{
    /// <summary>
    /// 试卷试题分组
    /// </summary>
    [Table("ExExaminationPaperQuestionGroups")]
    public class ExaminationPaperQuestionGroup : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxTipsLength = 512;
        public const int MaxNumberCodeLength = 64;
        public int? TenantId { get; set; }

        /// <summary>
        /// 试卷
        /// </summary>
        public virtual int ExaminationPaperId { get; set; }

        /// <summary>
        /// 试卷
        /// </summary>
        [ForeignKey(nameof(ExaminationPaperId))]
        public virtual ExaminationPaper ExaminationPaper { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        [Required]
        [MaxLength(MaxNameLength)]
        public virtual string Name { get; set; }

        /// <summary>
        /// 分组提示
        /// </summary>
        [MaxLength(MaxNameLength)]
        public virtual string Tips { get; set; }

        /// <summary>
        /// 编号
        /// </summary>
        [MaxLength(MaxNameLength)]
        public virtual string NumberCode { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public virtual int Order { get; set; }

        /// <summary>
        /// 试题类型
        /// </summary>
        public virtual string QuestionType { get; set; }
    }
}
