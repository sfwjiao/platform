using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Examination.TrialBalloon;

namespace Examination.Examination
{
    /// <summary>
    /// 试卷中的考题
    /// </summary>
    [Table("ExExaminationPaperQuestions")]
    public class ExaminationPaperQuestion : FullAuditedEntity<long>, IMayHaveTenant
    {
        public const int MaxPointsLength = 512;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// 所在试卷
        /// </summary>
        public virtual int ExaminationPaperId { get; set; }

        /// <summary>
        /// 所在试卷
        /// </summary>
        [ForeignKey(nameof(ExaminationPaperId))]
        public virtual ExaminationPaper ExaminationPaper { get; set; }

        /// <summary>
        /// 所在分组
        /// </summary>
        public virtual int ExaminationPaperQuestionGroupId { get; set; }

        /// <summary>
        /// 所在分组
        /// </summary>
        [ForeignKey(nameof(ExaminationPaperQuestionGroupId))]
        public virtual ExaminationPaperQuestionGroup ExaminationPaperQuestionGroup { get; set; }

        /// <summary>
        /// 科目
        /// </summary>
        public virtual int? SubjectUnitId { get; set; }

        /// <summary>
        /// 科目
        /// </summary>
        [ForeignKey(nameof(SubjectUnitId))]
        public virtual SubjectUnit SubjectUnit { get; set; }

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
        /// 分数
        /// </summary>
        public virtual double Score { get; set; }

        /// <summary>
        /// Parent <see cref="ExaminationPaperQuestion"/> Id.
        /// 根节点为Null
        /// </summary>
        public virtual long? ParentId { get; set; }

        /// <summary>
        /// Parent <see cref="ExaminationPaperQuestion"/>.
        /// 根节点为Null
        /// </summary>
        [ForeignKey(nameof(ParentId))]
        public virtual ExaminationPaperQuestion Parent { get; set; }

        /// <summary>
        /// 试题类型
        /// </summary>
        public virtual string QuestionType { get; protected set; }
    }
}
