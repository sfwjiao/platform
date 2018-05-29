using System.IO;
using System.Threading.Tasks;

namespace Platform.File
{
    public abstract class FileStorageBase
    {
        protected string SaveFromFileInfo(Stream stream, FileInfo fileInfo)
        {
            using (var fileStream = fileInfo.Open(FileMode.Create))
            {
                var bArr = new byte[1024];
                var size = stream.Read(bArr, 0, bArr.Length);
                while (size > 0)
                {
                    fileStream.Write(bArr, 0, size);
                    size = stream.Read(bArr, 0, bArr.Length);
                }
            }
            return fileInfo.Name;
        }

        protected async Task<string> SaveFromFileInfoAsync(Stream stream, FileInfo fileInfo)
        {
            using (var fileStream = fileInfo.Open(FileMode.Create))
            {
                var bArr = new byte[1024];
                var size = await stream.ReadAsync(bArr, 0, bArr.Length);
                while (size > 0)
                {
                    await fileStream.WriteAsync(bArr, 0, size);
                    size = await stream.ReadAsync(bArr, 0, bArr.Length);
                }
            }
            return fileInfo.Name;
        }

        protected Stream GetStreamFromFileInfo(FileInfo fileInfo)
        {
            var mstream = new MemoryStream();
            using (var fileStream = fileInfo.Open(FileMode.Open))
            {
                var bArr = new byte[1024];
                var size = fileStream.Read(bArr, 0, bArr.Length);
                while (size > 0)
                {
                    mstream.Write(bArr, 0, size);
                    size = fileStream.Read(bArr, 0, bArr.Length);
                }
            }

            mstream.Position = 0;
            return mstream;
        }

        protected async Task<Stream> GetStreamFromFileInfoAsync(FileInfo fileInfo)
        {
            var mstream = new MemoryStream();
            using (var fileStream = fileInfo.Open(FileMode.Open))
            {
                var bArr = new byte[1024];
                var size = await fileStream.ReadAsync(bArr, 0, bArr.Length);
                while (size > 0)
                {
                    await mstream.WriteAsync(bArr, 0, size);
                    size = await fileStream.ReadAsync(bArr, 0, bArr.Length);
                }
            }

            mstream.Position = 0;
            return mstream;
        }
    }
}
