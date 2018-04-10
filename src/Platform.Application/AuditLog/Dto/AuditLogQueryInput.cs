using Abp.Application.Services.Dto;
using System;

namespace Platform.AuditLog.Dto
{
    public class AuditLogQueryInput : QueryInput<long>
    {
        public string ServiceName { get; set; }
        public string MethodName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
