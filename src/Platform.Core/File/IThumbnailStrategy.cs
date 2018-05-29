using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Platform.File
{
    public interface IThumbnailStrategy
    {
        /// <summary>
        /// 计算缩略图真实尺寸
        /// </summary>
        /// <param name="originalSize">原图尺寸</param>
        /// <param name="thumbnailSize">缩略图尺寸</param>
        /// <returns>真实尺寸</returns>
        Size Calc(Size originalSize, Size thumbnailSize);

        /// <summary>
        /// 获取缩略图扩展名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetThumbnailExtension(string fileName);

        /// <summary>
        /// 是否需要缓存的图片尺寸
        /// </summary>
        bool NeedCacheSize(Size size);
    }
}
