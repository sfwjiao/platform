using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.AutoMapper;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;

namespace Abp.Application.Services
{
    /// <summary>
    /// 包含基础查询功能的基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TPrimaryKey"></typeparam>
    /// <typeparam name="TDto"></typeparam>
    /// <typeparam name="TListDto"></typeparam>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TQueryInput"></typeparam>
    public class DefaultActionApplicationService<
        TPrimaryKey,
        T,
        TDto,
        TListDto,
        TInput,
        TQueryInput
        >
        : ApplicationService
        , IDefaultActionApplicationService<TPrimaryKey, TDto, TListDto, TInput, TQueryInput>
        where TPrimaryKey : struct
        where T : Entity<TPrimaryKey>
        where TDto : EntityDto<TPrimaryKey>
        where TListDto : EntityDto<TPrimaryKey>
        where TInput : NullableIdDto<TPrimaryKey>
        where TQueryInput : QueryInput<TPrimaryKey>
    {
        protected readonly IRepository<T, TPrimaryKey> EntityRepository;
        public DefaultActionApplicationService(IRepository<T, TPrimaryKey> entityRepository)
        {
            EntityRepository = entityRepository;
        }

        [UnitOfWork]
        public virtual async Task Add(TInput input)
        {
            var entity = await OnAddExecuting(input);

            //执行插入数据方法
            await EntityRepository.InsertAsync(entity);
        }

        /// <summary>
        /// 添加执行前的附加业务
        /// </summary>
        /// <param name="input"></param>
        protected virtual async Task<T> OnAddExecuting(TInput input)
        {
            var entity = await Task.FromResult(input.MapTo<T>());

            if (entity is IMayHaveTenant)
            {
                var entityAsMayHaveTenant = entity.As<IMayHaveTenant>();
                entityAsMayHaveTenant.TenantId = AbpSession.TenantId;
            }

            return entity;
        }

        [UnitOfWork]
        public virtual async Task<TPrimaryKey> AddAndGetId(TInput input)
        {
            var entity = await OnAddAndGetIdExecuting(input);

            //执行插入数据方法
            return await EntityRepository.InsertAndGetIdAsync(entity);
        }

        /// <summary>
        /// 添加并返回主键执行前的附加业务
        /// </summary>
        /// <param name="input"></param>
        protected virtual async Task<T> OnAddAndGetIdExecuting(TInput input)
        {
            return await OnAddExecuting(input);
        }

        [UnitOfWork]
        public async Task<TDto> AddAndGetObj(TInput input)
        {
            var entity = await OnAddAndGetObjExecuting(input);

            //执行插入数据方法
            var id = await EntityRepository.InsertAndGetIdAsync(entity);
            UnitOfWorkManager.Current.SaveChanges();

            return (await EntityRepository.GetAsync(id)).MapTo<TDto>();
        }

        /// <summary>
        /// 添加并返回对象执行前的附加业务
        /// </summary>
        /// <param name="input"></param>
        protected virtual async Task<T> OnAddAndGetObjExecuting(TInput input)
        {
            return await OnAddExecuting(input);
        }

        public virtual async Task Delete(TPrimaryKey id)
        {
            await EntityRepository.DeleteAsync(id);
        }

        [UnitOfWork]
        public virtual async Task Edit(TInput input)
        {
            //验证主键
            if (!input.Id.HasValue) throw new UserFriendlyException("传入Id参数不正确！");

            //获取需要修改的对象
            var entity = await EntityRepository.GetAsync(input.Id.Value);

            //修改数据
            entity = await OnEditExecuting(input, entity);

            //执行修改数据方法
            await EntityRepository.UpdateAsync(entity);
        }

        /// <summary>
        /// 保存修改执行前的附加业务
        /// </summary>
        /// <param name="input"></param>
        /// <param name="entity"></param>
        protected virtual async Task<T> OnEditExecuting(TInput input, T entity)
        {
            input.MapTo(entity);
            return await Task.FromResult(entity);
        }

        public virtual async Task<TDto> Get(TPrimaryKey id)
        {
            var entity = await EntityRepository.GetAsync(id);

            entity = await OnGetExecuting(id, entity);

            return entity.MapTo<TDto>();
        }

        /// <summary>
        /// 获取数据执行前的附加业务
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        protected virtual async Task<T> OnGetExecuting(TPrimaryKey id, T entity)
        {
            return await Task.FromResult(entity);
        }

        public async Task<PagedResultDto<TListDto>> Query(TQueryInput input)
        {
            //验证参数
            if (!input.PageSize.HasValue) throw new UserFriendlyException("传入PageSize参数不正确！");
            if (!input.Start.HasValue) throw new UserFriendlyException("传入Start参数不正确！");

            //获取查询对象
            var query = EntityRepository.GetAll();

            //自定义条件
            query = OnCustomQueryWhere(query, input);

            //获取总数
            var totalcount = await Task.FromResult(query.Count());

            //排序
            query = OnQueryOrderBy(query, input);

            //添加分页条件
            query = query.Skip(input.Start.Value);
            if (input.PageSize.Value > 0)
            {
                query = query.Take(input.PageSize.Value);
            }

            //执行查询
            var obj = await Task.FromResult(query.ToList());

            //包装为分页输出对象
            return new PagedResultDto<TListDto>(totalcount, obj.MapTo<List<TListDto>>());
        }

        /// <summary>
        /// 查询数据自定义条件
        /// </summary>
        /// <param name="query"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        protected virtual IQueryable<T> OnCustomQueryWhere(IQueryable<T> query, TQueryInput input)
        {
            return query;
        }

        /// <summary>
        /// 查询数据排序
        /// </summary>
        /// <param name="query"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        protected virtual IQueryable<T> OnQueryOrderBy(IQueryable<T> query, TQueryInput input)
        {
            return query.OrderBy(x => x.Id);
        }
    }
}
