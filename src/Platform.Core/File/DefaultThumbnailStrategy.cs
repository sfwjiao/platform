using System.Drawing;

namespace Platform.File
{
    public class DefaultThumbnailStrategy : IThumbnailStrategy
    {
        public virtual Size Calc(Size originalSize, Size thumbnailSize)
        {
            var realSize = new Size();
            if (originalSize.Width > originalSize.Height)
            {
                if (originalSize.Width > thumbnailSize.Width) //如果原图的宽小于标准宽不压缩
                {
                    realSize.Width = thumbnailSize.Width;
                    realSize.Height = originalSize.Height * thumbnailSize.Width / originalSize.Width;
                }
            }
            else
            {
                if (originalSize.Height > thumbnailSize.Height)
                {
                    realSize.Height = thumbnailSize.Height;
                    realSize.Width = realSize.Height * originalSize.Width / originalSize.Height;
                }
            }
            if (originalSize.Width <= thumbnailSize.Width && originalSize.Height <= thumbnailSize.Height)
            {
                realSize.Width = originalSize.Width;
                realSize.Height = originalSize.Height;
            }
            return realSize;
        }

        public virtual string GetThumbnailExtension(string fileName)
        {
            return "jpg";
        }

        public bool NeedCacheSize(Size size)
        {
            return size.Width == 100 && size.Height== 100;
        }
    }
}
