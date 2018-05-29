using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    public interface IThumbnailStorage
    {
        Tuple<Stream, string> GetThumbnail(string fileName, Size size);

        Task<Tuple<Stream, string>> GetThumbnailAsync(string fileName, Size size);
    }
}
