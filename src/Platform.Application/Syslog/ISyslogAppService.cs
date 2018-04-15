using Abp.Application.Services;
using Platform.Syslog.Dto;

namespace Platform.Syslog
{
    public interface ISyslogAppService : IApplicationService, IDefaultActionApplicationService<long,
        SyslogDto,
        SyslogListDto,
        SyslogInput,
        SyslogQueryInput
        >
    {
    }
}
