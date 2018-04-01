using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Platform.MultiTenancy;

namespace Platform.Sessions.Dto
{
    [AutoMapFrom(typeof(Tenant))]
    public class TenantLoginInfoDto : EntityDto
    {
        public string TenancyName { get; set; }

        public string Name { get; set; }
    }
}