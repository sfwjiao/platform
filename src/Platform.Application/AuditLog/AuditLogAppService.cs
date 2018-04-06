using System.Linq;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Platform.AuditLog.Dto;

namespace Platform.AuditLog
{
    public class AuditLogAppService : DefaultActionApplicationService<
        long,
        Abp.Auditing.AuditLog,
        AuditLogDto,
        AuditLogListDto,
        AuditLogInput,
        AuditLogQueryInput
        >, IAuditLogAppService
    {
        public AuditLogAppService(IRepository<Abp.Auditing.AuditLog, long> entityRepository) : base(entityRepository)
        {

        }

        protected override IQueryable<Abp.Auditing.AuditLog> OnQueryOrderBy(IQueryable<Abp.Auditing.AuditLog> query, AuditLogQueryInput input)
        {
            return query.OrderByDescending(x => x.ExecutionTime).ThenBy(x => x.Id);
        }
    }
}
