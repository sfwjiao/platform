using Abp.Application.Services;
using Platform.Syslog.Dto;

namespace Platform.Syslog
{
    public interface ISyslogAppService : IAsyncCrudAppService<SyslogDto, SyslogListDto, long,
        SyslogQueryInput,
        SyslogCreateInput,
        SyslogInput,
        SyslogInput,
        SyslogInput
        >
    {
    }
}
