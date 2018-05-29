using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    public interface IFileInfoResolver
    {
        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns></returns>
        FileInfo GetFileInfo(Stream stream, string extension);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns></returns>
        Task<FileInfo> GetFileInfoAsync(Stream stream, string extension);
        
        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="stream">图片流</param>
        /// <param name="extension">图片扩展名</param>
        /// <returns></returns>
        FileInfo GetImageFileInfo(Stream stream, string extension);

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="stream">图片流</param>
        /// <param name="extension">图片扩展名</param>
        /// <returns></returns>
        Task<FileInfo> GetImageFileInfoAsync(Stream stream, string extension);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns></returns>
        FileInfo GetFileInfo(string fileName);
        
        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns></returns>
        Task<FileInfo> GetFileInfoAsync(string fileName);

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="fileName">图片名称</param>
        /// <returns></returns>
        FileInfo GetImageFileInfo(string fileName);

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="fileName">图片名称</param>
        /// <returns></returns>
        Task<FileInfo> GetImageFileInfoAsync(string fileName);
    }
}
