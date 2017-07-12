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
    [Table("ExSubjectUnits")]
    public class SubjectUnit : FullAuditedEntity, IMayHaveTenant
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
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public virtual ICollection<SubjectUnit> Children { get; set; }


        /// <summary>
        /// 初始化一个新实例 <see cref="SubjectUnit"/> class.
        /// </summary>
        public SubjectUnit()
        {

        }

        /// <summary>
        /// 初始化一个新实例 <see cref="SubjectUnit"/> class.
        /// </summary>
        /// <param name="tenantId">租户编号</param>
        /// <param name="displayName">科目名称</param>
        /// <param name="parentId">父级科目，根节点的父级为Null</param>
        public SubjectUnit(int? tenantId, string displayName, int? parentId = null)
        {
            TenantId = tenantId;
            DisplayName = displayName;
            ParentId = parentId;
        }

        /// <summary>
        /// 根据传入数字创建编号.
        /// 例如: 传入参数为 4,2 返回 "00004.00002";
        /// </summary>
        /// <param name="numbers">数字</param>
        public static string CreateCode(params int[] numbers)
        {
            if (numbers.IsNullOrEmpty())
            {
                return null;
            }

            return numbers.Select(number => number.ToString(new string('0', CodeUnitLength))).JoinAsString(".");
        }

        /// <summary>
        /// 合并编号
        /// 例如: 如果 parentCode = "00001", childCode = "00042" 返回 "00001.00042".
        /// </summary>
        /// <param name="parentCode">父级编号. 如果是父级根节点可为Null或Empty.</param>
        /// <param name="childCode">子级编号.</param>
        public static string AppendCode(string parentCode, string childCode)
        {
            if (childCode.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(childCode), "childCode can not be null or empty.");
            }

            if (parentCode.IsNullOrEmpty())
            {
                return childCode;
            }

            return parentCode + "." + childCode;
        }

        /// <summary>
        /// 获取编码的相对值.
        /// 例如: 如果 code = "00019.00055.00001" and parentCode = "00019" 返回 "00055.00001".
        /// </summary>
        /// <param name="code">编码.</param>
        /// <param name="parentCode">父级编码.</param>
        public static string GetRelativeCode(string code, string parentCode)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            if (parentCode.IsNullOrEmpty())
            {
                return code;
            }

            if (code.Length == parentCode.Length)
            {
                return null;
            }

            return code.Substring(parentCode.Length + 1);
        }

        /// <summary>
        /// 计算下一个编号.
        /// 例如: 如果 code = "00019.00055.00001" 返回 "00019.00055.00002".
        /// </summary>
        /// <param name="code">编码.</param>
        public static string CalculateNextCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            var parentCode = GetParentCode(code);
            var lastUnitCode = GetLastUnitCode(code);

            return AppendCode(parentCode, CreateCode(Convert.ToInt32(lastUnitCode) + 1));
        }

        /// <summary>
        /// 获得叶子节点的编码.
        /// 例如: 如果 code = "00019.00055.00001" 返回 "00001".
        /// </summary>
        /// <param name="code">编码.</param>
        public static string GetLastUnitCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            var splittedCode = code.Split('.');
            return splittedCode[splittedCode.Length - 1];
        }

        /// <summary>
        /// 获得父级编码.
        /// 例如: 如果 code = "00019.00055.00001" 返回 "00019.00055".
        /// </summary>
        /// <param name="code">编码.</param>
        public static string GetParentCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            var splittedCode = code.Split('.');
            if (splittedCode.Length == 1)
            {
                return null;
            }

            return splittedCode.Take(splittedCode.Length - 1).JoinAsString(".");
        }

        /// <summary>
        /// 计算父级科目的最新子级编号
        /// </summary>
        /// <param name="parentSubjectUnit">父级科目</param>
        /// <param name="lastChild">子级中编号最大的子科目</param>
        /// <returns></returns>
        public static string CalculateCode(SubjectUnit parentSubjectUnit, SubjectUnit lastChild)
        {
            var parentCode = parentSubjectUnit?.Code;
            return lastChild == null ? AppendCode(parentCode, CreateCode(1)) : CalculateNextCode(lastChild.Code);
        }
    }
}
