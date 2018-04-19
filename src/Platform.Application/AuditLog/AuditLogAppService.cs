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
using Platform.Authorization;
using Abp.Authorization;

namespace Platform.AuditLog
{
    [AbpAuthorize(PermissionNames.Platform)]
    public class AuditLogAppService : DefaultActionApplicationService<
        long,
        Abp.Auditing.AuditLog,
        AuditLogDto,
        AuditLogListDto,
        AuditLogInput,
        AuditLogQueryInput
        >, IAuditLogAppService
    {

        public TenantManager TenantManager { get; set; }

        public UserManager UserManager { get; set; }

        public AuditLogAppService(IRepository<Abp.Auditing.AuditLog, long> entityRepository) : base(entityRepository)
        {

        }

        protected override async Task<AuditLogDto> OnGetExecuted(long id, AuditLogDto dto)
        {
            if (dto.TenantId.HasValue)
                dto.Tenant = (await TenantManager.GetByIdAsync(dto.TenantId.Value)).MapTo<TenantListDto>();

            if (dto.UserId.HasValue)
                dto.User = (await UserManager.FindByIdAsync(dto.UserId.Value)).MapTo<UserListDto>();

            return dto;
        }

        protected override IQueryable<Abp.Auditing.AuditLog> OnCustomQueryWhere(IQueryable<Abp.Auditing.AuditLog> query, AuditLogQueryInput input)
        {
            query = base.OnCustomQueryWhere(query, input);

            if (!input.MethodName.IsNullOrEmpty())
            {
                query = query.Where(x => x.MethodName == input.MethodName);
            }
            if(!input.ServiceName.IsNullOrEmpty())
            {
                query = query.Where(x => x.ServiceName.StartsWith(input.ServiceName) || x.ServiceName.EndsWith(input
                    .ServiceName));
            }
            if(input.StartDate.HasValue)
            {
                var startDate = input.StartDate.Value.Date;
                query = query.Where(x => x.ExecutionTime >= startDate);
            }
            if(input.EndDate.HasValue)
            {
                var endDate = input.EndDate.Value.Date.AddDays(1);
                query = query.Where(x => x.ExecutionTime <= endDate);
            }
            return query;
        }

        protected override IQueryable<Abp.Auditing.AuditLog> OnQueryOrderBy(IQueryable<Abp.Auditing.AuditLog> query, AuditLogQueryInput input)
        {
            return query.OrderByDescending(x => x.ExecutionTime).ThenBy(x => x.Id);
        }
    }
}
