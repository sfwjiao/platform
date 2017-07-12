using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace Examination.TrialBalloonServices.Points.Dto
{
    [AutoMapFrom(typeof(TrialBalloon.Points))]
    public class PointsDto: EntityDto
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
        /// 显示内容
        /// </summary>
        public string PointsContent { get; set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public virtual ICollection<PointsDto> Children { get; set; }
    }
}
