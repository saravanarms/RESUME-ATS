using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeAnalyzer.Domain.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Infrastructure.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly string? _connectionString;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            _connectionString = configuration.GetConnectionString("AzureBlobStorage");
            _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "resumes";
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

            if (string.IsNullOrEmpty(_connectionString) || _connectionString.Contains("UseDevelopmentStorage=true") || _connectionString == "Your_Azure_Blob_Storage_Connection_String")
            {
                // Fallback for Local Development / Testing without Azure Config
                _logger.LogWarning("Azure Blob Storage connection string is missing or is set to development placeholder. Saving file to local temporary storage instead.");
                
                var localFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(localFolder))
                {
                    Directory.CreateDirectory(localFolder);
                }

                var localPath = Path.Combine(localFolder, uniqueFileName);
                using (var localStream = new FileStream(localPath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(localStream);
                }

                // Return a local URL
                return $"/uploads/{uniqueFileName}";
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Create container if it doesn't exist
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient(uniqueFileName);
                
                fileStream.Position = 0;
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

                _logger.LogInformation($"Successfully uploaded {fileName} to Azure Blob Storage: {blobClient.Uri}");
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {fileName} to Azure Blob Storage.");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl)) return false;

            // Handle local development fallback deletion
            if (blobUrl.StartsWith("/uploads/"))
            {
                var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", blobUrl.TrimStart('/'));
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                    return true;
                }
                return false;
            }

            if (string.IsNullOrEmpty(_connectionString) || _connectionString == "Your_Azure_Blob_Storage_Connection_String")
            {
                return false;
            }

            try
            {
                var uri = new Uri(blobUrl);
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Extract blob name from URL
                var blobName = Path.GetFileName(uri.LocalPath);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete blob {blobUrl} from Azure Storage.");
                return false;
            }
        }
    }
}
