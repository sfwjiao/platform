using Abp.Dependency;
using Abp.Domain.Repositories;
using System.Linq;

namespace Platform.Log
{
    public class SyslogManager : ITransientDependency
    {
        public const string SyslogDebugLevel = "DEBUG";
        public const string SyslogInfoLevel = "INFO";
        public const string SyslogWarnLevel = "WARN";
        public const string SyslogErrorLevel = "ERROR";
        public const string SyslogFatalLevel = "FATAL";

        private IRepository<Syslog, long> _syslogRepository;

        public SyslogManager(IRepository<Syslog, long> syslogRepository)
        {
            _syslogRepository = syslogRepository;
        }

        public IQueryable<Syslog> GetAllByLevel(string level)
        {
            var query = _syslogRepository.GetAll();
            return query.Where(x =>
                level == SyslogDebugLevel ? true
                : level == SyslogInfoLevel ? x.Level != SyslogDebugLevel
                : level == SyslogWarnLevel ? x.Level != SyslogDebugLevel && x.Level != SyslogInfoLevel
                : level == SyslogErrorLevel ? x.Level == SyslogErrorLevel || x.Level == SyslogFatalLevel
                : level == SyslogFatalLevel ? x.Level == SyslogFatalLevel : false
            );
        }
    }
}
