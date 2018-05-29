using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Platform.File;

namespace Platform.Web.Controllers
{
    public class FileUploadController : PlatformControllerBase
    {
        private readonly IFileStorage _fileStorage;
        public FileUploadController(IFileStorage fileStorage)
        {
            _fileStorage = fileStorage;
        }

        // GET: FileUpload
        public ActionResult Index()
        {
            return View();
        }
    }
}