using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Domain.Repositories;
using Platform.AuditLog.Dto;

namespace Platform.AuditLog
{
    public class AuditLogAppService : DefaultActionApplicationService<
        long,
        AuditLogInclude,
        AuditLogDto,
        AuditLogListDto,
        AuditLogInput,
        AuditLogQueryInput
        >, IAuditLogAppService
    {
        public AuditLogAppService(IRepository<AuditLogInclude, long> entityRepository) : base(entityRepository)
        {

        }

        protected override IQueryable<AuditLogInclude> OnQueryOrderBy(IQueryable<AuditLogInclude> query, AuditLogQueryInput input)
        {
            return query.OrderByDescending(x => x.ExecutionTime).ThenBy(x => x.Id);
        }
    }
}
