using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BeatRender
{
    public record PlaylistEntry
    {
        public int TrackNumber { get; set; }
        public string TrackTitle { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public decimal BPM { get; set; }
        public string Time { get; set; }
        public string Key { get; set; }
        public string DateAdded { get; set; }
    }
    public class PlaylistParser
    {
        private readonly ILogger<PlaylistParser> _logger;

        public PlaylistParser(ILogger<PlaylistParser> logger)
        {
            _logger = logger;
        }

        [Function(nameof(PlaylistParser))]
        [BlobOutput("playlists-parsed/playlist.json")]
        public async Task<string> Run([BlobTrigger("playlists/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();

            var lines = content.Split("\n", System.StringSplitOptions.RemoveEmptyEntries);
            var playlistEntries = new List<PlaylistEntry>();

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split("\t");
                if (columns.Length >= 9)
                {
                    playlistEntries.Add(new PlaylistEntry()
                    {
                        TrackNumber = int.Parse(columns[0]),
                        TrackTitle = columns[2],
                        Artist = columns[3],
                        Album = columns[4],
                        Genre = columns[5],
                    });
                }
            }
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name}");

            return System.Text.Json.JsonSerializer.Serialize<List<PlaylistEntry>>(playlistEntries);

        }
    }
}
