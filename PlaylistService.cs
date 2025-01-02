using Azure.Storage.Blobs;
using System.Reflection.Metadata;
using System.Text;

namespace BeatRender
{
    public class PlaylistService
    {
        private readonly string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _parsedPlaylistContainerName = Environment.GetEnvironmentVariable("parsed-playlist-container");
        private readonly string _rawPlaylistContainer = Environment.GetEnvironmentVariable("raw-playlist-container");

        public async Task<string> GetLatestPlaylistJsonAsync()
        {
            // Create a BlobServiceClient
            var blobServiceClient = new BlobServiceClient(_connectionString);

            // Get the container client for the 'playlists-parsed' container
            var containerClient = blobServiceClient.GetBlobContainerClient(_parsedPlaylistContainerName);

            // Ensure the container exists
            if (!await containerClient.ExistsAsync())
            {
                throw new Exception($"Container '{_parsedPlaylistContainerName}' does not exist.");
            }

            // List all blobs in the container and order by creation date descending
            var blobs = containerClient.GetBlobs()
                .OrderByDescending(blob => blob.Properties.CreatedOn);

            // Get the latest blob
            var latestBlob = blobs.FirstOrDefault();
            if (latestBlob == null)
            {
                throw new Exception("No blobs found in the container.");
            }

            // Get the blob client for the latest blob
            var blobClient = containerClient.GetBlobClient(latestBlob.Name);

            // Download the blob content
            MemoryStream memoryStream = new MemoryStream();
            var blobDownload = await blobClient.DownloadToAsync(memoryStream);
            // Reset the memory stream position to the beginning
            memoryStream.Seek(0, SeekOrigin.Begin);
            string jsonContent = Encoding.UTF8.GetString(memoryStream.ToArray());

            return jsonContent; // Return the JSON content
        }

        public async Task DeleteLatestPlayListsAsync()
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var rawContainerClient = blobServiceClient.GetBlobContainerClient(_rawPlaylistContainer);
            var parsedContainerClient = blobServiceClient.GetBlobContainerClient(_parsedPlaylistContainerName);

            var allParsedPlaylists = parsedContainerClient.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.All);
            var allRawPlaylists = rawContainerClient.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.All);
        
            await foreach (var blob in allRawPlaylists)
            {
                rawContainerClient.DeleteBlob(blob.Name);
            }

            await foreach (var blob in allParsedPlaylists)
            {
                parsedContainerClient.DeleteBlob(blob.Name);
            }


        }
    }
}
