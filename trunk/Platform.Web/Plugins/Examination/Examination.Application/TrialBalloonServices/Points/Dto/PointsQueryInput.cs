using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;

namespace Examination.TrialBalloonServices.Points.Dto
{
    public class PointsQueryInput:QueryInput
    {
        public int? ParentId { get; set; }
    }
}
