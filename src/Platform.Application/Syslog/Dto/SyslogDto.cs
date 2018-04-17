using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace Platform.Syslog.Dto
{
    [AutoMap(typeof(Log.Syslog))]
    public class SyslogDto : EntityDto<long>
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 线程号
        /// </summary>
        public string Thread { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 产生日志的类
        /// </summary>
        public string Logger { get; set; }

        /// <summary>
        /// 日志信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string Exception { get; set; }
    }
}
