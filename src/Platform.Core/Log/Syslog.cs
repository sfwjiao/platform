using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Log
{
    /// <summary>
    /// log4日志
    /// </summary>
    [Table("PlatSyslogs")]
    public class Syslog : Entity<long>, IHasCreationTime
    {
        /// <summary>
        /// Maximum length of the <see cref="Thread"/> property.
        /// </summary>
        public const int MaxThreadLength = 256;

        /// <summary>
        /// Maximum length of the <see cref="Level"/> property.
        /// </summary>
        public const int MaxLevelLength = 256;

        /// <summary>
        /// Maximum length of the <see cref="Logger"/> property.
        /// </summary>
        public const int MaxLoggerLength = 256;

        /// <summary>
        /// Maximum length of the <see cref="Message"/> property.
        /// </summary>
        public const int MaxMessageLength = 2048;

        /// <summary>
        /// Maximum length of the <see cref="Exception"/> property.
        /// </summary>
        public const int MaxExceptionLength = 2048;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 线程号
        /// </summary>
        [StringLength(MaxThreadLength)]
        public string Thread { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        [StringLength(MaxLevelLength)]
        public string Level { get; set; }

        /// <summary>
        /// 产生日志的类
        /// </summary>
        [StringLength(MaxLoggerLength)]
        public string Logger { get; set; }
        
        /// <summary>
        /// 日志信息
        /// </summary>
        [StringLength(MaxMessageLength)]
        public string Message { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        [StringLength(MaxExceptionLength)]
        public string Exception { get; set; }
    }
}
