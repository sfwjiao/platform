using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Configuration;

namespace Platform.File
{
    public class DefaultExtensionValidator : IExtensionValidator
    {
        private readonly ISettingManager _settingManager;

        public DefaultExtensionValidator(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        public void FileCheck(string extension)
        {
            extension = extension.TrimStart('.');

            var allowedExtensions =
                _settingManager.GetSettingValue(
                    PlatformConsts.ApplicationConfigSettingNames.UploadFileAllowedExtensions);

            if (allowedExtensions.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries).All(x => x.TrimStart('.') != extension))
            {
                throw new NotAllowedExtensionException(extension);
            }
        }

        public async Task FileCheckAsync(string extension)
        {
            extension = extension.TrimStart('.');

            var allowedExtensions =
                await _settingManager.GetSettingValueAsync(
                    PlatformConsts.ApplicationConfigSettingNames.UploadFileAllowedExtensions);

            if (allowedExtensions.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).All(x => x.TrimStart('.') != extension))
            {
                throw new NotAllowedExtensionException(extension);
            }
        }

        public void ImageCheck(string extension)
        {
            extension = extension.TrimStart('.');

            var allowedExtensions =
                _settingManager.GetSettingValue(
                    PlatformConsts.ApplicationConfigSettingNames.UploadImageAllowedExtensions);

            if (allowedExtensions.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).All(x => x.TrimStart('.') != extension))
            {
                throw new NotAllowedExtensionException(extension);
            }
        }

        public async Task ImageCheckAsync(string extension)
        {
            extension = extension.TrimStart('.');

            var allowedExtensions =
                await _settingManager.GetSettingValueAsync(
                    PlatformConsts.ApplicationConfigSettingNames.UploadImageAllowedExtensions);

            if (allowedExtensions.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).All(x => x.TrimStart('.') != extension))
            {
                throw new NotAllowedExtensionException(extension);
            }
        }
    }
}
