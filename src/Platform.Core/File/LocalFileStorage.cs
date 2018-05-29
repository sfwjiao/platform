using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    /// <summary>
    /// 本地文件存储器
    /// </summary>
    public class LocalFileStorage : FileStorageBase, IFileStorage
    {
        private readonly IFileInfoResolver _fileInfoResolver;
        private readonly IThumbnailStrategy _thumbnailStrategy;
        private readonly IExtensionValidator _extensionValidator;
        private readonly IThumbnailStorage _thumbnailStorage;

        public LocalFileStorage(IFileInfoResolver resolver,
            IThumbnailFileInfoResolver thumbnailFileInfoResolver,
            IThumbnailStrategy thumbnailStrategy,
            IExtensionValidator extensionValidator,
            IThumbnailStorage thumbnailStorage)
        {
            _fileInfoResolver = resolver;
            _thumbnailStrategy = thumbnailStrategy;
            _extensionValidator = extensionValidator;
            _thumbnailStorage = thumbnailStorage;
        }

        public string Save(Stream stream, string extension)
        {
            _extensionValidator.FileCheck(extension);

            var fileInfo = _fileInfoResolver.GetFileInfo(stream, extension);
            if (fileInfo.Exists) return fileInfo.Name;

            return SaveFromFileInfo(stream, fileInfo);
        }

        public async Task<string> SaveAsync(Stream stream, string extension)
        {
            await _extensionValidator.FileCheckAsync(extension);

            var fileInfo = await _fileInfoResolver.GetFileInfoAsync(stream, extension);
            if (fileInfo.Exists) return fileInfo.Name;

            return await SaveFromFileInfoAsync(stream, fileInfo);
        }

        public string SaveImage(Stream stream, string extension)
        {
            _extensionValidator.ImageCheck(extension);

            var fileInfo = _fileInfoResolver.GetImageFileInfo(stream, extension);
            if (fileInfo.Exists) return fileInfo.Name;

            return SaveFromFileInfo(stream, fileInfo);
        }

        public async Task<string> SaveImageAsync(Stream stream, string extension)
        {
            await _extensionValidator.ImageCheckAsync(extension);

            var fileInfo = await _fileInfoResolver.GetImageFileInfoAsync(stream, extension);
            if (fileInfo.Exists) return fileInfo.Name;

            return await SaveFromFileInfoAsync(stream, fileInfo);
        }

        public Tuple<Stream, string> Get(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
            _extensionValidator.FileCheck(extension);

            var fileInfo = _fileInfoResolver.GetFileInfo(fileName);
            if (fileInfo.Exists) return null;

            return Tuple.Create(GetStreamFromFileInfo(fileInfo), extension);
        }

        public async Task<Tuple<Stream, string>> GetAsync(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
            await _extensionValidator.FileCheckAsync(extension);

            var fileInfo = await _fileInfoResolver.GetFileInfoAsync(fileName);
            if (!fileInfo.Exists) return null;

            return Tuple.Create(await GetStreamFromFileInfoAsync(fileInfo), extension);
        }

        public Tuple<Stream, string> GetImage(string fileName, Size? size = null)
        {
            var fileInfo = _fileInfoResolver.GetImageFileInfo(fileName);
            if (!fileInfo.Exists) return null;

            var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
            if (!size.HasValue) return Tuple.Create(GetStreamFromFileInfo(fileInfo), extension);

            return _thumbnailStorage.GetThumbnail(fileName, size.Value);
        }

        public async Task<Tuple<Stream, string>> GetImageAsync(string fileName, Size? size = null)
        {
            var fileInfo = await _fileInfoResolver.GetImageFileInfoAsync(fileName);
            if (!fileInfo.Exists) return null;

            var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower();
            if (!size.HasValue) return Tuple.Create(await GetStreamFromFileInfoAsync(fileInfo), extension);

            return await _thumbnailStorage.GetThumbnailAsync(fileName, size.Value);
        }
    }
}
