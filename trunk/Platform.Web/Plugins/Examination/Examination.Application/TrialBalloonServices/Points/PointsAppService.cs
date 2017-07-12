using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Examination.TrialBalloon;
using Examination.TrialBalloonServices.Points.Dto;

namespace Examination.TrialBalloonServices.Points
{
    public class PointsAppService : DefaultActionApplicationService<int, TrialBalloon.Points, PointsDto, PointsSimpleDto, PointsInput, PointsQueryInput>, IPointsAppService
    {
        public PointsAppService(IRepository<TrialBalloon.Points, int> entityRepository) : base(entityRepository)
        {
        }

        protected override async Task<TrialBalloon.Points> OnAddExecuting(PointsInput input)
        {
            var entity = await base.OnAddExecuting(input);

            //计算Code编号
            var parentPoints = await EntityRepository.FirstOrDefaultAsync(x => x.Id == input.ParentId);
            var parentId = parentPoints?.Id;

            var getLastQuery = EntityRepository.GetAll().OrderByDescending(x => x.Code);
            var lastPointsWithSameParent = await Task.FromResult(getLastQuery.FirstOrDefault(x => x.ParentId == parentId));

            entity.Code = TrialBalloon.Points.CalculateCode(parentPoints, lastPointsWithSameParent);

            return entity;
        }

        protected override async Task<TrialBalloon.Points> OnAddAndGetIdExecuting(PointsInput input)
        {
            return await OnAddExecuting(input);
        }

        protected override async Task<TrialBalloon.Points> OnAddAndGetObjExecuting(PointsInput input)
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

        protected override IQueryable<TrialBalloon.Points> OnCustomQueryWhere(IQueryable<TrialBalloon.Points> query, PointsQueryInput input)
        {
            return query.Where(x => x.ParentId == input.ParentId);
        }

        protected override IQueryable<TrialBalloon.Points> OnQueryOrderBy(IQueryable<TrialBalloon.Points> query, PointsQueryInput input)
        {
            var orderQuery = query.OrderBy(x => x.CreationTime);
            return base.OnQueryOrderBy(orderQuery, input);
        }

        protected override IQueryable<TrialBalloon.Points> OnCustomQuerySimpleWhere(IQueryable<TrialBalloon.Points> query, PointsQueryInput input)
        {
            return OnCustomQueryWhere(query, input);
        }

        protected override IQueryable<TrialBalloon.Points> OnQuerySimpleOrderBy(IQueryable<TrialBalloon.Points> query, PointsQueryInput input)
        {
            return OnQueryOrderBy(query, input);
        }

        Task IPointsAppService.DeletaByCode(string code)
        {
            throw new NotImplementedException();
        }
    }
}
