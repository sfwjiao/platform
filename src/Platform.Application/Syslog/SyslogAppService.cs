using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Platform.Log;
using Platform.Syslog.Dto;
using System.Linq;

namespace Platform.Syslog
{
    public class SyslogAppService : AsyncCrudAppService<Log.Syslog, SyslogDto, SyslogListDto, long,
        SyslogQueryInput,
        SyslogCreateInput,
        SyslogInput,
        SyslogInput,
        SyslogInput
        >, ISyslogAppService
    {
        private readonly SyslogManager _syslogManager;

        public SyslogAppService(
            IRepository<Log.Syslog, long> entityRepository,
            SyslogManager syslogManager
            ) : base(entityRepository)
        {
            _syslogManager = syslogManager;
        }

        protected override IQueryable<Log.Syslog> CreateFilteredQuery(SyslogQueryInput input)
        {
            var query = base.CreateFilteredQuery(input);

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

        protected override IQueryable<Log.Syslog> ApplySorting(IQueryable<Log.Syslog> query, SyslogQueryInput input)
        {
            return query.OrderByDescending(x => x.CreationTime).ThenBy(x => x.Id);
        }
    }
}
