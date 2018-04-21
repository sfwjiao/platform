using Abp.Application.Services;
using Platform.AuditLog.Dto;

namespace Platform.AuditLog
{
    public interface IAuditLogAppService : IAsyncCrudAppService<AuditLogDto,
        long,
        GetAllInput,
        AuditLogCreateInput,
        AuditLogInput,
        AuditLogInput,
        AuditLogInput
        >
    {
    }
}
