using Abp.Application.Services;
using Platform.AuditLog.Dto;

namespace Platform.AuditLog
{
    public interface IAuditLogAppService: IApplicationService, IDefaultActionApplicationService<long,
        AuditLogDto,
        AuditLogListDto,
        AuditLogInput,
        AuditLogQueryInput
        >
    {
    }
}
