using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SingleProjectTemplate.Custom
{
    /// <summary>
    /// 客户表
    /// </summary>
    [Table("PtCustomers")]
    public class Customer : FullAuditedEntity<long>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        /// <summary>
        /// 客户姓名
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// 客户电话
        /// </summary>
        public string Tel { get; set; }

        /// <summary>
        /// 客户年龄
        /// </summary>
        public int? Age { get; set; }
    }
}
