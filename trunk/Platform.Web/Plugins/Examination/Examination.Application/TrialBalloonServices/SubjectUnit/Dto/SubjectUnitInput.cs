using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace Examination.TrialBalloonServices.SubjectUnit.Dto
{
    [AutoMap(typeof(TrialBalloon.SubjectUnit))]
    public class SubjectUnitInput : NullableIdDto
    {
        /// <summary>
        /// 根节点
        /// </summary>
        public int? ParentId { get; set; }
        /// <summary>
        /// 层级代码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 显示名称
        /// </summary>
        [Required]
        public string DisplayName { get; set; }
    }
}
