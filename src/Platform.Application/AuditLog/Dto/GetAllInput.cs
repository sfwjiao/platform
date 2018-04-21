using System;
using Abp.Application.Services.Dto;

namespace Platform.AuditLog.Dto
{
    public class GetAllInput : PagedResultRequestDto
    {
        public string ServiceName { get; set; }

        public string MethodName { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
