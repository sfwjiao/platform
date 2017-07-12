using System.Threading.Tasks;
using Abp.Application.Services;
using Examination.TrialBalloonServices.Points.Dto;

namespace Examination.TrialBalloonServices.Points
{
    public interface IPointsAppService:IApplicationService,IDefaultActionApplicationService<int,PointsDto,PointsSimpleDto,PointsInput,PointsQueryInput>
    {
        Task DeletaByCode(string code);
    }
}
