using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace Examination.TrialBalloonServices.SubjectUnit.Dto
{
    [AutoMapFrom(typeof(TrialBalloon.SubjectUnit))]
    public class SubjectUnitSimpleDto : EntityDto
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
        public string DisplayName { get; set; }
    }
}
