using Abp.Dependency;
using Abp.Extensions;
using System.Collections.Generic;
using System.Reflection;

namespace Abp.Auditing
{
    /// <summary>
    /// Default implementation of <see cref="IAuditInfoProvider" />.
    /// </summary>
    public class DefaultAuditInfoProvider : IAuditInfoProvider, ITransientDependency
    {
        public IClientInfoProvider ClientInfoProvider { get; set; }

        public DefaultAuditInfoProvider()
        {
            ClientInfoProvider = NullClientInfoProvider.Instance;
        }

        public virtual void Fill(AuditInfo auditInfo, MethodInfo method, IDictionary<string, object> arguments)
        {
            if (auditInfo.ClientIpAddress.IsNullOrEmpty())
            {
                auditInfo.ClientIpAddress = ClientInfoProvider.ClientIpAddress;
            }

            if (auditInfo.BrowserInfo.IsNullOrEmpty())
            {
                auditInfo.BrowserInfo = ClientInfoProvider.BrowserInfo;
            }

            if (auditInfo.ClientName.IsNullOrEmpty())
            {
                auditInfo.ClientName = ClientInfoProvider.ComputerName;
            }
        }
    }
}