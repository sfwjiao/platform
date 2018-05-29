using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Abp.Extensions;
using Abp.UI;
using Platform.File;
using Platform.Extensions;

namespace Platform.Web.Controllers
{
    public class FileController : PlatformControllerBase
    {
        private readonly IFileStorage _fileStorage;
        public FileController(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        public async Task<ActionResult> Upload(HttpPostedFileBase file)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName);
                var fileName = await _fileStorage.SaveAsync(file.InputStream, extension);
                return Content(fileName);
            }
            catch (NotAllowedExtensionException ex)
            {
                throw new UserFriendlyException($"不允许上传{ex.Extension.WithDefaultValue("无扩展名的")}文件");
            }
        }

        public async Task<ActionResult> Download(string fileName, string dname)
        {
            try
            {
                if (string.IsNullOrEmpty(dname)) dname = fileName;
                var fileResult = await _fileStorage.GetAsync(fileName);
                if (fileResult.Item1 == null) return HttpNotFound();
                return File(fileResult.Item1, "application/octet-stream", dname);
            }
            catch (NotAllowedExtensionException)
            {
                return HttpNotFound();
            }
        }

        public async Task<ActionResult> UploadImage(HttpPostedFileBase file)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName);
                var imgName = await _fileStorage.SaveImageAsync(file.InputStream, extension);
                return Content(imgName);
            }
            catch (NotAllowedExtensionException ex)
            {
                throw new UserFriendlyException($"不允许上传{ex.Extension.WithDefaultValue("无扩展名的")}图片");
            }
        }

        public async Task<ActionResult> DownloadImage(string fileName, string dname)
        {
            try
            {
                var imageResult = await _fileStorage.GetImageAsync(fileName);

                if (imageResult.Item1 == null) return HttpNotFound();
                return File(imageResult.Item1, "application/octet-stream", dname);
            }
            catch (NotAllowedExtensionException)
            {
                return HttpNotFound();
            }
        }

        public async Task<ActionResult> ViewImage(string fileName, int? w)
        {
            try
            {
                var imageResult = await _fileStorage.GetImageAsync(fileName, w.HasValue ? new Size(w.Value, w.Value) : (Size?)null);

                if (imageResult.Item1 == null) return HttpNotFound();
                return File(imageResult.Item1, $"image/{imageResult.Item2}");
            }
            catch (NotAllowedExtensionException)
            {
                return HttpNotFound();
            }
        }
    }
}