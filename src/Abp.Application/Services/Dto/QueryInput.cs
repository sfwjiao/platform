namespace Abp.Application.Services.Dto
{
    public class QueryInput<TPrimaryKey> : NullableIdDto<TPrimaryKey> where TPrimaryKey : struct
    {
        /// <summary>
        /// 每页显示数量
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// 起始查询位置
        /// </summary>
        public int? Start { get; set; }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string KeyWord { get; set; }
    }

    public class QueryInput : QueryInput<int>
    {
    }
}
