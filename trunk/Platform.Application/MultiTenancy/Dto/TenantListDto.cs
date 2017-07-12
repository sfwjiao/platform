using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Platform.MultiTenancy;

namespace Platform.MultiTenancy.Dto
{
    [AutoMapFrom(typeof(Tenant))]
    public class TenantListDto : EntityDto
    {
        public string TenancyName { get; set; }

        public string Name { get; set; }
    }
}