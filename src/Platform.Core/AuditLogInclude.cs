using Abp.Auditing;
using Platform.MultiTenancy;
using Platform.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform
{
    public class AuditLogInclude : AuditLog
    {
        [ForeignKey(nameof(TenantId))]
        public virtual Tenant Tenant { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
    }
}
