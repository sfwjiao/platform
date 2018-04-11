using System.Threading.Tasks;
using Abp.Application.Services.Dto;

namespace Abp.Application.Services
{
    public interface IDefaultActionApplicationService<TPrimaryKey,
        TDto,
        TListDto,
        in TInput,
        in TQueryInput
        >
        where TPrimaryKey : struct
        where TDto : EntityDto<TPrimaryKey>
        where TListDto : EntityDto<TPrimaryKey>
        where TInput : NullableIdDto<TPrimaryKey>
        where TQueryInput : QueryInput<TPrimaryKey>
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="input">传入参数</param>
        /// <returns></returns>
        Task Add(TInput input);

        /// <summary>
        /// 添加并返回主键
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<TPrimaryKey> AddAndGetId(TInput input);

        /// <summary>
        /// 添加并返回新建数据
        /// </summary>
        /// <param name="input">传入参数</param>
        /// <returns></returns>
        Task<TDto> AddAndGetObj(TInput input);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">主键</param>
        /// <returns></returns>
        Task Delete(TPrimaryKey id);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="input">传入参数</param>
        /// <returns></returns>
        Task Edit(TInput input);

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="id">主键</param>
        /// <returns></returns>
        Task<TDto> Get(TPrimaryKey id);

        /// <summary>
        /// 查询数据集
        /// </summary>
        /// <param name="input">查询条件参数</param>
        /// <returns></returns>
        Task<PagedResultDto<TListDto>> Query(TQueryInput input);
    }
}
