using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web.Hosting;
using Abp.Configuration;
using Abp.Extensions;
using Abp.IO;

namespace Platform.File
{
    public class DefaultThumbnailFileInfoResolver : IThumbnailFileInfoResolver
    {
        private readonly ISettingManager _settingManager;
        private readonly IThumbnailStrategy _thumbnailStrategy;

        public DefaultThumbnailFileInfoResolver(ISettingManager settingManager,
            IThumbnailStrategy thumbnailStrategy)
        {
            _settingManager = settingManager;
            _thumbnailStrategy = thumbnailStrategy;
        }
        public FileInfo GetThumbnailFileInfo(string fileName, Size size)
        {
            var fileNameWithOutExtension = fileName.Left(fileName.Length - Path.GetExtension(fileName).Length);
            var thumbnailFileName =
                $"{fileNameWithOutExtension}_{size.Width}_{size.Height}.{_thumbnailStrategy.GetThumbnailExtension(fileName)}";

            var directoryPath =
                _settingManager.GetSettingValue(PlatformConsts.ApplicationConfigSettingNames.UploadImagePath);

            var directoryServerPath = HostingEnvironment.MapPath($"~/{directoryPath.TrimStart('~').TrimStart('/')}");
            DirectoryHelper.CreateIfNotExists(directoryServerPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            var filePath = Path.Combine(directoryServerPath, thumbnailFileName);
            return new FileInfo(filePath);
        }

        public async Task<FileInfo> GetThumbnailFileInfoAsync(string fileName, Size size)
        {
            var fileNameWithOutExtension = fileName.Left(fileName.Length - Path.GetExtension(fileName).Length);
            var thumbnailFileName =
                $"{fileNameWithOutExtension}_{size.Width}_{size.Height}.{_thumbnailStrategy.GetThumbnailExtension(fileName)}";

            var directoryPath = await 
                _settingManager.GetSettingValueAsync(PlatformConsts.ApplicationConfigSettingNames.UploadImagePath);

            var directoryServerPath = HostingEnvironment.MapPath($"~/{directoryPath.TrimStart('~').TrimStart('/')}");
            DirectoryHelper.CreateIfNotExists(directoryServerPath);

            // ReSharper disable once AssignNullToNotNullAttribute
            var filePath = Path.Combine(directoryServerPath, thumbnailFileName);
            return new FileInfo(filePath);
        }
    }
}
