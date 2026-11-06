using Azure.Storage.Blobs;
namespace AgriStream.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("AzureStorage");

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            var container = _blobServiceClient.GetBlobContainerClient("drone-image");

            await container.CreateIfNotExistsAsync();

            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            var blob = container.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();

            await blob.UploadAsync(stream, true);

            return fileName;
        }
    }
}
