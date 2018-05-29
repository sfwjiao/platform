using System.Threading.Tasks;

namespace Platform.File
{
    public interface IExtensionValidator
    {
        /// <summary>
        /// 验证扩展名是否被允许
        /// 如果不允许，抛出<see cref="NotAllowedExtensionException"/>异常
        /// </summary>
        /// <param name="extension">扩展名</param>
        void FileCheck(string extension);

        /// <summary>
        /// 验证扩展名是否被允许
        /// 如果不允许，抛出<see cref="NotAllowedExtensionException"/>异常
        /// </summary>
        /// <param name="extension">扩展名</param>
        Task FileCheckAsync(string extension);
        
        /// <summary>
        /// 验证扩展名是否被允许
        /// 如果不允许，抛出<see cref="NotAllowedExtensionException"/>异常
        /// </summary>
        /// <param name="extension">扩展名</param>
        void ImageCheck(string extension);

        /// <summary>
        /// 验证扩展名是否被允许
        /// 如果不允许，抛出<see cref="NotAllowedExtensionException"/>异常
        /// </summary>
        /// <param name="extension">扩展名</param>
        Task ImageCheckAsync(string extension);
    }
}
