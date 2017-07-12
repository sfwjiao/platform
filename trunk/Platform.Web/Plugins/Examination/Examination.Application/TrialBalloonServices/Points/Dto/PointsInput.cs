using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examination.TrialBalloonServices.Points.Dto
{
    [AutoMapFrom(typeof(TrialBalloon.Points))]
    public class PointsInput: NullableIdDto
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
        [Required]
        public string PointsContent { get; set; }
    }
}
