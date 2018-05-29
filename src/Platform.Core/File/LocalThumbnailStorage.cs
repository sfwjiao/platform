using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    public class LocalThumbnailStorage : FileStorageBase, IThumbnailStorage
    {
        private readonly IThumbnailFileInfoResolver _thumbnailFileInfoResolver;
        private readonly IFileInfoResolver _fileInfoResolver;
        private readonly IThumbnailStrategy _thumbnailStrategy;

        public LocalThumbnailStorage(IThumbnailFileInfoResolver thumbnailFileInfoResolver,
            IThumbnailStrategy thumbnailStrategy,
            IFileInfoResolver fileInfoResolver)
        {
            _thumbnailFileInfoResolver = thumbnailFileInfoResolver;
            _thumbnailStrategy = thumbnailStrategy;
            _fileInfoResolver = fileInfoResolver;
        }

        public Tuple<Stream, string> GetThumbnail(string fileName, Size size)
        {
            var thumbnailFileInfo = _thumbnailFileInfoResolver.GetThumbnailFileInfo(fileName, size);
            if (thumbnailFileInfo.Exists) return Tuple.Create(GetStreamFromFileInfo(thumbnailFileInfo), thumbnailFileInfo.Extension.TrimStart('.').ToLower());
            
            var fileInfo = _fileInfoResolver.GetFileInfo(fileName);
            var extension = _thumbnailStrategy.GetThumbnailExtension(fileName);
            var stream = CreateThumbnail(fileInfo, size, GetImageFormat(extension));

            if (_thumbnailStrategy.NeedCacheSize(size))
            {
                SaveFromFileInfo(stream, thumbnailFileInfo);
                stream.Position = 0;
            }

            return Tuple.Create(stream, extension);
        }

        public async Task<Tuple<Stream, string>> GetThumbnailAsync(string fileName, Size size)
        {
            var thumbnailFileInfo = await _thumbnailFileInfoResolver.GetThumbnailFileInfoAsync(fileName, size);
            if (thumbnailFileInfo.Exists) return Tuple.Create(await GetStreamFromFileInfoAsync(thumbnailFileInfo), thumbnailFileInfo.Extension.TrimStart('.').ToLower());

            var fileInfo = await _fileInfoResolver.GetFileInfoAsync(fileName);
            var extension = _thumbnailStrategy.GetThumbnailExtension(fileName);
            var stream = CreateThumbnail(fileInfo, size, GetImageFormat(extension));

            if (_thumbnailStrategy.NeedCacheSize(size))
            {
                await SaveFromFileInfoAsync(stream, thumbnailFileInfo);
                stream.Position = 0;
            }

            return Tuple.Create(stream, extension);
        }

        public Stream CreateThumbnail(FileInfo fileInfo, Size thumbnailSize, ImageFormat thumbnailFormat)
        {
            var originalImage = Image.FromFile(fileInfo.FullName);

            var realSize = _thumbnailStrategy.Calc(originalImage.Size, thumbnailSize);

            //新建一个bmp图片
            var thumbnailImage = new Bitmap(realSize.Width, realSize.Height);
            //新建一个画板
            var graphic = Graphics.FromImage(thumbnailImage);
            //设置高质量查值法
            graphic.InterpolationMode = InterpolationMode.High;
            //设置高质量，低速度呈现平滑程度
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            //清空画布并以透明背景色填充
            graphic.Clear(Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分
            graphic.DrawImage(originalImage, new Rectangle(0, 0, realSize.Width, realSize.Height), new Rectangle(0, 0, originalImage.Size.Width, originalImage.Size.Height), GraphicsUnit.Pixel);

            var stream = new MemoryStream();
            thumbnailImage.Save(stream, thumbnailFormat);
            stream.Position = 0;
            return stream;
        }

        protected virtual ImageFormat GetImageFormat(string extension)
        {
            return ImageFormat.Jpeg;
        }
    }
}
