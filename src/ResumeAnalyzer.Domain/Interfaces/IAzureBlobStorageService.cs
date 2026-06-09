using System;
using System.IO;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Domain.Interfaces
{
    public interface IAzureBlobStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteFileAsync(string blobUrl);
    }
}
