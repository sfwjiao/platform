using Abp.AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;

namespace Examination.TrialBalloonServices.Points.Dto
{
    [AutoMapFrom(typeof(TrialBalloon.Points))]
    public class PointsSimpleDto:EntityDto
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
    }
}
