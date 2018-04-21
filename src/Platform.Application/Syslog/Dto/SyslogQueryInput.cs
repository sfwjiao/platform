using Abp.Application.Services.Dto;
using System;

namespace Platform.Syslog.Dto
{
    public class SyslogQueryInput : PagedResultRequestDto
    {
        public string Level { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
