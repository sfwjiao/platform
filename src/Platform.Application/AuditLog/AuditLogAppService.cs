using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Platform.AuditLog.Dto;
using Platform.MultiTenancy;
using Platform.Users;
using Abp.AutoMapper;
using Platform.MultiTenancy.Dto;
using Platform.Users.Dto;
using Abp.Extensions;

namespace Platform.AuditLog
{
    public class AuditLogAppService : AsyncCrudAppService<Abp.Auditing.AuditLog, AuditLogDto,
        long,
        GetAllInput,
        AuditLogCreateInput,
        AuditLogInput,
        AuditLogInput,
        AuditLogInput
        >, IAuditLogAppService
    {

        public TenantManager TenantManager { get; set; }

        public UserManager UserManager { get; set; }

        public AuditLogAppService(IRepository<Abp.Auditing.AuditLog, long> entityRepository) : base(entityRepository)
        {

        }

        public override async Task<AuditLogDto> Get(AuditLogInput input)
        {
            var dto = await base.Get(input);

            if (dto.TenantId.HasValue)
                dto.Tenant = (await TenantManager.GetByIdAsync(dto.TenantId.Value)).MapTo<TenantListDto>();

            if (dto.UserId.HasValue)
                dto.User = (await UserManager.FindByIdAsync(dto.UserId.Value)).MapTo<UserListDto>();

            return dto;
        }

        protected override IQueryable<Abp.Auditing.AuditLog> CreateFilteredQuery(GetAllInput input)
        {
            var query = base.CreateFilteredQuery(input);

            if (!input.MethodName.IsNullOrEmpty())
            {
                query = query.Where(x => x.MethodName == input.MethodName);
            }
            if (!input.ServiceName.IsNullOrEmpty())
            {
                query = query.Where(x => x.ServiceName.StartsWith(input.ServiceName) || x.ServiceName.EndsWith(input
                                             .ServiceName));
            }
            if (input.StartDate.HasValue)
            {
                var startDate = input.StartDate.Value.Date;
                query = query.Where(x => x.ExecutionTime >= startDate);
            }
            if (input.EndDate.HasValue)
            {
                var endDate = input.EndDate.Value.Date.AddDays(1);
                query = query.Where(x => x.ExecutionTime <= endDate);
            }
            return query;
        }

        protected override IQueryable<Abp.Auditing.AuditLog> ApplySorting(IQueryable<Abp.Auditing.AuditLog> query, GetAllInput input)
        {
            return query.OrderByDescending(x => x.ExecutionTime).ThenBy(x => x.Id);
        }
    }
}
