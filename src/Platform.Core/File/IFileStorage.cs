using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    /// <summary>
    /// 文件存储器
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文件名称</returns>
        string Save(Stream stream, string extension);

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文件名称</returns>
        Task<string> SaveAsync(Stream stream, string extension);

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="stream">保存流</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文件名称</returns>
        string SaveImage(Stream stream, string extension);

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="stream">保存流</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文件名称</returns>
        Task<string> SaveImageAsync(Stream stream, string extension);

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns>文件流, 扩展名</returns>
        Tuple<Stream, string> Get(string fileName);

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns>文件流, 扩展名</returns>
        Task<Tuple<Stream, string>> GetAsync(string fileName);

        /// <summary>
        /// 读取图片
        /// </summary>
        /// <param name="fileName">图片名称</param>
        /// <param name="size">缩略图尺寸</param>
        /// <returns>图片流, 扩展名</returns>
        Tuple<Stream, string> GetImage(string fileName, Size? size = null);

        /// <summary>
        /// 读取图片
        /// </summary>
        /// <param name="fileName">图片名称</param>
        /// <param name="size">缩略图尺寸</param>
        /// <returns>图片流, 扩展名</returns>
        Task<Tuple<Stream, string>> GetImageAsync(string fileName, Size? size = null);
    }
}
