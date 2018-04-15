using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Extensions;
using System;

namespace Platform.Syslog.Dto
{
    [AutoMap(typeof(Log.Syslog))]
    public class SyslogListDto : EntityDto<long>
    {
        public const int DisplayMessageLength = 50;

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
        private string message;
        public string Message
        {
            get
            {
                return message.Length < DisplayMessageLength
                    ? message
                    : $"{message.Left(DisplayMessageLength)}...";
            }
            set { message = value; }
        }
    }
}
