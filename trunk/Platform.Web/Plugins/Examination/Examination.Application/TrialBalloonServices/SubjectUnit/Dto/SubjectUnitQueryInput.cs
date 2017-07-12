using Abp.Application.Services.Dto;

namespace Examination.TrialBalloonServices.SubjectUnit.Dto
{
    public class SubjectUnitQueryInput : QueryInput
    {
        public int? ParentId { get; set; }
    }
}
