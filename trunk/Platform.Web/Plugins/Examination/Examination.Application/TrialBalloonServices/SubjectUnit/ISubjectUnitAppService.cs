using System.Threading.Tasks;
using Abp.Application.Services;
using Examination.TrialBalloonServices.SubjectUnit.Dto;

namespace Examination.TrialBalloonServices.SubjectUnit
{
    public interface ISubjectUnitAppService : IApplicationService
        , IDefaultActionApplicationService<int,
        SubjectUnitDto,
        SubjectUnitSimpleDto,
        SubjectUnitInput,
        SubjectUnitQueryInput
        >
    {
        Task DeleteByCode(string code);
    }
}
