using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Platform.MultiTenancy.Dto;
using Platform.Users.Dto;

namespace Platform.AuditLog.Dto
{
    [AutoMapFrom(typeof(Abp.Auditing.AuditLog))]
    public class AuditLogDto : EntityDto<long>
    {
        public string BrowserInfo { get; set; }
        public string ClientIpAddress { get; set; }
        public string ClientName { get; set; }
        public string CustomData { get; set; }
        public string Exception { get; set; }
        public DateTime ExecutionTime { get; set; }
        public int? ImpersonatorTenantId { get; set; }
        public long? ImpersonatorUserId { get; set; }
        public string MethodName { get; set; }
        public string Parameters { get; set; }
        public string ServiceName { get; set; }
        public TenantListDto Tenant { get; set; }
        public UserListDto User { get; set; }
    }
}
