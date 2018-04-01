using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace SingleProjectTemplate.CustomService.Customer.Dto
{
    [AutoMapFrom(typeof(Custom.Customer))]
    public class CustomerDto : EntityDto<long>
    {
        /// <summary>
        /// 客户姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 客户电话
        /// </summary>
        public string Tel { get; set; }

        /// <summary>
        /// 客户年龄
        /// </summary>
        public int? Age { get; set; }
    }
}
