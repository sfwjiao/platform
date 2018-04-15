using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Platform.Log;
using Platform.Syslog.Dto;
using System.Linq;

namespace Platform.Syslog
{
    public class SyslogAppService : DefaultActionApplicationService<
        long,
        Log.Syslog,
        SyslogDto,
        SyslogListDto,
        SyslogInput,
        SyslogQueryInput
        >, ISyslogAppService
    {
        private SyslogManager _syslogManager { get; set; }

        public SyslogAppService(
            IRepository<Log.Syslog, long> entityRepository,
            SyslogManager syslogManager
            ) : base(entityRepository)
        {
            _syslogManager = syslogManager;
        }

        protected override IQueryable<Log.Syslog> OnCustomQueryWhere(IQueryable<Log.Syslog> query, SyslogQueryInput input)
        {
            if (!input.Level.IsNullOrEmpty())
            {
                query = _syslogManager.GetAllByLevel(input.Level);
            }
            if (input.StartDate.HasValue)
            {
                var startDate = input.StartDate.Value.Date;
                query = query.Where(x => x.CreationTime >= startDate);
            }
            if (input.EndDate.HasValue)
            {
                var endDate = input.EndDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreationTime <= endDate);
            }
            return query;
        }

        protected override IQueryable<Log.Syslog> OnQueryOrderBy(IQueryable<Log.Syslog> query, SyslogQueryInput input)
        {
            return query.OrderByDescending(x => x.CreationTime).ThenBy(x => x.Id);
        }
    }
}
