using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Hosting;
using Abp.Configuration;
using Abp.Extensions;
using Abp.IO;
using Abp.IO.Extensions;

namespace Platform.File
{
    public class DefaultFileInfoResolver : IFileInfoResolver
    {
        private readonly ISettingManager _settingManager;

        public DefaultFileInfoResolver(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        public FileInfo GetFileInfo(Stream stream, string extension)
        {
            var base64String = Convert.ToBase64String(stream.GetAllBytes());
            var fileName = $"{base64String.ToMd5().ToLower()}.{extension.TrimStart('.').ToLower()}";
            stream.Position = 0;

            return GetFileInfo(fileName);
        }

        public async Task<FileInfo> GetFileInfoAsync(Stream stream, string extension)
        {
            var base64String = Convert.ToBase64String(stream.GetAllBytes());
            var fileName = $"{base64String.ToMd5().ToLower()}.{extension.TrimStart('.').ToLower()}";
            stream.Position = 0;

            return await GetFileInfoAsync(fileName);
        }

        public FileInfo GetImageFileInfo(Stream stream, string extension)
        {
            var base64String = Convert.ToBase64String(stream.GetAllBytes());
            var fileName = $"{base64String.ToMd5().ToLower()}.{extension.TrimStart('.').ToLower()}";
            stream.Position = 0;

            return GetImageFileInfo(fileName);
        }

        public async Task<FileInfo> GetImageFileInfoAsync(Stream stream, string extension)
        {
            var base64String = Convert.ToBase64String(stream.GetAllBytes());
            var fileName = $"{base64String.ToMd5().ToLower()}.{extension.TrimStart('.').ToLower()}";
            stream.Position = 0;

            return await GetImageFileInfoAsync(fileName);
        }

        public FileInfo GetFileInfo(string fileName)
        {
            var directoryPath =
                _settingManager.GetSettingValue(PlatformConsts.ApplicationConfigSettingNames.UploadFilePath);

            var directoryServerPath = HostingEnvironment.MapPath($"~/{directoryPath.TrimStart('~').TrimStart('/')}");
            DirectoryHelper.CreateIfNotExists(directoryServerPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            var filePath = Path.Combine(directoryServerPath, fileName);
            return new FileInfo(filePath);
        }

        public async Task<FileInfo> GetFileInfoAsync(string fileName)
        {
            var directoryPath = await
                _settingManager.GetSettingValueAsync(PlatformConsts.ApplicationConfigSettingNames.UploadFilePath);

            var directoryServerPath = HostingEnvironment.MapPath($"~/{directoryPath.TrimStart('~').TrimStart('/')}");
            DirectoryHelper.CreateIfNotExists(directoryServerPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            var filePath = Path.Combine(directoryServerPath, fileName);

            return new FileInfo(filePath);
        }

        public FileInfo GetImageFileInfo(string fileName)
        {
            var directoryPath =
                _settingManager.GetSettingValue(PlatformConsts.ApplicationConfigSettingNames.UploadImagePath);

            var directoryServerPath = HostingEnvironment.MapPath($"~/{directoryPath.TrimStart('~').TrimStart('/')}");
            DirectoryHelper.CreateIfNotExists(directoryServerPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            var filePath = Path.Combine(directoryServerPath, fileName);
            return new FileInfo(filePath);
        }

        public async Task<FileInfo> GetImageFileInfoAsync(string fileName)
        {
            var directoryPath = await
                _settingManager.GetSettingValueAsync(PlatformConsts.ApplicationConfigSettingNames.UploadImagePath);

            var directoryServerPath = HostingEnvironment.MapPath($"~/{directoryPath.TrimStart('~').TrimStart('/')}");
            DirectoryHelper.CreateIfNotExists(directoryServerPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            var filePath = Path.Combine(directoryServerPath, fileName);

            return new FileInfo(filePath);
        }
    }
}
