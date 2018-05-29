using System;
using System.Runtime.Serialization;
using Abp;

namespace Platform.File
{
    [Serializable]
    public class NotAllowedExtensionException : AbpException
    {
        /// <summary>
        /// 扩展名
        /// </summary>
        public string Extension { get; set; }
        
        /// <summary>
        /// Creates a new <see cref="NotAllowedExtensionException"/> object.
        /// </summary>
        public NotAllowedExtensionException()
        {

        }

        /// <summary>
        /// Creates a new <see cref="AbpException"/> object.
        /// </summary>
        public NotAllowedExtensionException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extension"></param>
        public NotAllowedExtensionException(string extension)
        {
            Extension = extension;
        }

        /// <summary>
        /// Creates a new <see cref="AbpException"/> object.
        /// </summary>
        /// <param name="extension">extension</param>
        /// <param name="message">Exception message</param>
        public NotAllowedExtensionException(string extension, string message)
            : base(message)
        {
            Extension = extension;
        }

        /// <summary>
        /// Creates a new <see cref="AbpException"/> object.
        /// </summary>
        /// <param name="extension">extension</param>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public NotAllowedExtensionException(string extension, string message, Exception innerException)
            : base(message, innerException)
        {
            Extension = extension;
        }
    }
}
