using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    public interface IThumbnailFileInfoResolver
    {

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="size">缩略图尺寸</param>
        /// <returns></returns>
        FileInfo GetThumbnailFileInfo(string fileName, Size size);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="size">缩略图尺寸</param>
        /// <returns></returns>
        Task<FileInfo> GetThumbnailFileInfoAsync(string fileName, Size size);
    }
}
