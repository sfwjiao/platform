using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 试题
    /// </summary>
    [Table("ExExaminationQuestions")]
    public class ExaminationQuestion : FullAuditedEntity<long>, IMayHaveTenant
    {
        public const int MaxNumberCodeLength = 64;
        public const int MaxPointsLength = 512;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 所在题库编号
        /// </summary>
        public virtual int ExaminationQuestionLibraryId { get; set; }

        /// <summary>
        /// 所在题库
        /// </summary>
        [ForeignKey(nameof(ExaminationQuestionLibraryId))]
        public virtual ExaminationQuestionLibrary ExaminationQuestionLibrary { get; set; }

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
        /// 编号
        /// </summary>
        [MaxLength(MaxNumberCodeLength)]
        public virtual string NumberCode { get; set; }

        /// <summary>
        /// 题干
        /// </summary>
        [Required]
        [Column(TypeName = "text")]
        public virtual string Stem { get; set; }

        /// <summary>
        /// 考点
        /// </summary>
        [StringLength(MaxPointsLength)]
        public virtual string Points { get; set; }

        /// <summary>
        /// 试题分析
        /// </summary>
        public virtual string Analysis { get; set; }

        /// <summary>
        /// Parent <see cref="ExaminationQuestion"/> Id.
        /// 根节点为Null
        /// </summary>
        public virtual long? ParentId { get; set; }

        /// <summary>
        /// Parent <see cref="ExaminationQuestion"/>.
        /// 根节点为Null
        /// </summary>
        [ForeignKey(nameof(ParentId))]
        public virtual ExaminationQuestion Parent { get; set; }

        /// <summary>
        /// 试题类型
        /// </summary>
        public virtual string QuestionType { get; protected set; }
    }
}

