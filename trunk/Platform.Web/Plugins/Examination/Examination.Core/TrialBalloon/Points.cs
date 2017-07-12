using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Abp.Collections.Extensions;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Extensions;

namespace Examination.TrialBalloon
{
    /// <summary>
    /// 科目
    /// </summary>
    [Table("ExPoints")]
    public class Points : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxDisplayNameLength = 128;
        public const int MaxDepth = 16;
        public const int CodeUnitLength = 5;
        public const int MaxCodeLength = MaxDepth * (CodeUnitLength + 1) - 1;
        public virtual int? TenantId { get; set; }

        /// <summary>
        /// Parent <see cref="SubjectUnit"/>.
        /// 根节点为Null
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual SubjectUnit Parent { get; set; }

        /// <summary>
        /// Parent <see cref="SubjectUnit"/> Id.
        /// 根节点为Null
        /// </summary>
        public virtual int? ParentId { get; set; }

        public static string CalculateCode(Points parentPoints, Points lastSubjectUnitWithSameParent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 层级代码
        /// 例如: "00001.00042.00005".
        /// 针对每个租户唯一.
        /// </summary>
        [Required]
        [StringLength(MaxCodeLength)]
        public virtual string Code { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [Required]
        [StringLength(MaxDisplayNameLength)]
        public virtual string PointsContent { get; set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public virtual ICollection<SubjectUnit> Children { get; set; }


        /// <summary>
        /// 初始化一个新实例 <see cref="Points"/> class.
        /// </summary>
        public Points()
        {

        }

        /// <summary>
        /// 初始化一个新实例 <see cref="SubjectUnit"/> class.
        /// </summary>
        /// <param name="tenantId">租户编号</param>
        /// <param name="pointsContent">科目名称</param>
        /// <param name="parentId">父级科目，根节点的父级为Null</param>
        public Points(int? tenantId, string pointsContent, int? parentId = null)
        {
            TenantId = tenantId;
            PointsContent = pointsContent;
            ParentId = parentId;
        }

        
    }
}
