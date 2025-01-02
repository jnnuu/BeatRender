using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BeatRender
{
    public class YoutubeUploader
    {
        private readonly ILogger<YoutubeUploader> _logger;
        private IConfiguration configuration;

        public YoutubeUploader(ILogger<YoutubeUploader> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
        }

        [Function(nameof(YoutubeUploader))]
        public async Task Run([BlobTrigger("beatrender-output/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();

            // Read the blob into a local temporary file
            var tempFilePath = Path.Combine(Path.GetTempPath(), name);

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await blobStreamReader.BaseStream.CopyToAsync(fileStream);
            }
            // Authenticate with YouTube API
            var youtubeService = await GetYouTubeService();

            var metadata = TagLib.File.Create(tempFilePath);
            var title = $"{metadata.Tag.Track.ToString("D3")} - {metadata.Tag.Title} ({metadata.Tag.FirstGenre} mix)";

            // Prepare video metadata
            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = title,
                    Description = "Visualized and uploaded with BeatRender (https://github.com/jnnuu/BeatRender)",
                    Tags = new[] { "DJ", "BeatRender", $"{metadata.Tag.FirstGenre}" },
                    CategoryId = "10" // Category ID for "Music"
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = "private"
                }
            };

            var videoId = "";

            // Upload the video
            using (var fileStream = new FileStream(tempFilePath, FileMode.Open))
            {
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += progress =>
                {
                    _logger.LogInformation($"Upload progress: {progress.Status} - {progress.BytesSent} bytes sent.");
                };
                videosInsertRequest.ResponseReceived += videoResponse =>
                {
                    _logger.LogInformation($"Video uploaded. Video ID: {videoResponse.Id}");
                    videoId = videoResponse.Id;
                };


                await videosInsertRequest.UploadAsync();
            }

            // Clean up temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            _logger.LogInformation($"C# Blob Trigger processed blob\n Name: {name} \n Youtube url: https://www.youtube.com/watch?v="+videoId);
        }


        private async Task<YouTubeService> GetYouTubeService()
        {
            var credentialPath = Environment.GetEnvironmentVariable("YouTubeCredentialsPath");
            if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
            {
                throw new FileNotFoundException("YouTube API credentials file not found.");
            }

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromFile(credentialPath).Secrets,
                new[] { YouTubeService.Scope.YoutubeUpload },
                "user",
                CancellationToken.None
            );

            return new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "BeatRender YouTube Uploader"
            });
        }


        //private async Task<YouTubeService> GetYouTubeService()
        //{
        //    // Path to the service account key file
        //    var credentialPath = Environment.GetEnvironmentVariable("YouTubeServiceAccountKeyPath");
        //    if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
        //    {
        //        throw new FileNotFoundException("Service account key file not found.");
        //    }

        //    // Authenticate using the service account
        //    GoogleCredential credential = GoogleCredential.FromFile(credentialPath)
        //        .CreateScoped(new[] { YouTubeService.Scope.YoutubeUpload });

        //    return new YouTubeService(new BaseClientService.Initializer
        //    {
        //        HttpClientInitializer = credential,
        //        ApplicationName = "BeatRender YouTube Uploader"
        //    });
        //}

    }
}
