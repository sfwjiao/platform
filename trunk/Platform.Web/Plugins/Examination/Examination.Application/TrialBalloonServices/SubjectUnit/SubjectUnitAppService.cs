using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Examination.TrialBalloonServices.SubjectUnit.Dto;

namespace Examination.TrialBalloonServices.SubjectUnit
{
    public class SubjectUnitAppService : DefaultActionApplicationService<
        int,
        TrialBalloon.SubjectUnit,
        SubjectUnitDto,
        SubjectUnitSimpleDto,
        SubjectUnitInput,
        SubjectUnitQueryInput
        >
        , ISubjectUnitAppService
    {
        public SubjectUnitAppService(IRepository<TrialBalloon.SubjectUnit, int> entityRepository) : base(entityRepository)
        {

        }

        protected override async Task<TrialBalloon.SubjectUnit> OnAddExecuting(SubjectUnitInput input)
        {
            var entity = await base.OnAddExecuting(input);

            //计算Code编号
            var parentSubjectUnit = await EntityRepository.FirstOrDefaultAsync(x => x.Id == input.ParentId);
            var parentId = parentSubjectUnit?.Id;

            var getLastQuery = EntityRepository.GetAll().OrderByDescending(x => x.Code);
            var lastSubjectUnitWithSameParent = await Task.FromResult(getLastQuery.FirstOrDefault(x => x.ParentId == parentId));

            entity.Code = TrialBalloon.SubjectUnit.CalculateCode(parentSubjectUnit, lastSubjectUnitWithSameParent);

            return entity;
        }

        protected override async Task<TrialBalloon.SubjectUnit> OnAddAndGetIdExecuting(SubjectUnitInput input)
        {
            return await OnAddExecuting(input);
        }

        protected override async Task<TrialBalloon.SubjectUnit> OnAddAndGetObjExecuting(SubjectUnitInput input)
        {
            return await OnAddExecuting(input);
        }


        public override async Task Delete(int id)
        {
            //根据Code层级代码删除当前subjectUnit以及子subjectUnit
            var subjectUnit = EntityRepository.Get(id);
            await EntityRepository.DeleteAsync(x => x.Code.StartsWith(subjectUnit.Code));
        }

        public async Task DeleteByCode(string code)
        {
            //根据Code层级代码删除当前subjectUnit以及子subjectUnit
            await EntityRepository.DeleteAsync(x => x.Code.StartsWith(code));
        }

        protected override IQueryable<TrialBalloon.SubjectUnit> OnCustomQueryWhere(IQueryable<TrialBalloon.SubjectUnit> query, SubjectUnitQueryInput input)
        {
            return query.Where(x => x.ParentId == input.ParentId);
        }

        protected override IQueryable<TrialBalloon.SubjectUnit> OnQueryOrderBy(IQueryable<TrialBalloon.SubjectUnit> query, SubjectUnitQueryInput input)
        {
            var orderQuery = query.OrderBy(x => x.CreationTime);
            return base.OnQueryOrderBy(orderQuery, input);
        }

        protected override IQueryable<TrialBalloon.SubjectUnit> OnCustomQuerySimpleWhere(IQueryable<TrialBalloon.SubjectUnit> query, SubjectUnitQueryInput input)
        {
            return OnCustomQueryWhere(query, input);
        }

        protected override IQueryable<TrialBalloon.SubjectUnit> OnQuerySimpleOrderBy(IQueryable<TrialBalloon.SubjectUnit> query, SubjectUnitQueryInput input)
        {
            return OnQueryOrderBy(query, input);
        }
    }
}
