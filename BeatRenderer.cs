using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Xabe.FFmpeg;

namespace BeatRender
{
    public class BeatRenderer
    {
        private readonly ILogger<BeatRenderer> _logger;

        public BeatRenderer(ILogger<BeatRenderer> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BeatRenderer))]
        //[BlobOutput("beatrender-output/{name}.mp4", Connection = "")]
        [BlobOutput("beatrender-output/video.mp4", Connection = "")]
        public async Task<Byte[]> Run([BlobTrigger("beatrender-input/{name}", Connection = "")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            var inputFilePath = Path.Combine(name);
            var outputFilePath = Path.Combine(name.Replace(".mp3",$"{Guid.NewGuid()}.mp4"));
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            // create input file
            using (var fileStream = new FileStream(inputFilePath, FileMode.Create, FileAccess.Write))
            {
                await blobStreamReader.BaseStream.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Wrote file {inputFilePath}");
            _logger.LogInformation($"File size: {new FileInfo(inputFilePath).Length} bytes");

            // Pass the file to FFmpeg
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputFilePath);
            var audioStream = mediaInfo.AudioStreams.First();

            // Get mp3 metadata
            var metadata = TagLib.File.Create(inputFilePath);
            var artist = metadata.Tag.Performers.FirstOrDefault();
            var year =  metadata.Tag.Year;
            var track = metadata.Tag.Title;
            var genre = metadata.Tag.Genres.FirstOrDefault();
            var number = metadata.Tag.Track.ToString("D3");
            var color = metadata.Tag.Comment;

            if (color == string.Empty) 
                color = "Orange";

            var text = "";

            if (artist != string.Empty) text += artist + "\n";
            if (number != "0") text += number + " - ";
            if (track != string.Empty) text += track + "\n";
            if (genre != string.Empty) text += genre;

            _logger.LogInformation($"\n{artist}\n{number} - {track}\n{genre}\n({year})");

            _logger.LogInformation($"Media duration: {mediaInfo.Duration}");

            var altConversion = await FFmpeg.Conversions.FromSnippet.ToMp4(inputFilePath, outputFilePath);
            string visualizerFilter = $"[0:a]showfreqs=mode=bar:s=1920x1080:colors={color}|{color}|{color}:win_size=2048:win_func=hanning:fscale=log:ascale=cbrt [v]";
            string textOverlayFilter = $"[v]drawtext=fontsize=80:x=100:y=100:fontcolor={color}:text=\'{text}\' [final]";
            string filterComplex = $"{visualizerFilter},{textOverlayFilter}";

            _logger.LogWarning($"\n{filterComplex}");

            altConversion.OnProgress += async (sender, args) =>
            {
                var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                await Console.Out.WriteLineAsync($"[{args.Duration} / {args.TotalLength}] {percent}%");
            };

            altConversion.SetFrameRate(30.00);
            altConversion.AddParameter($"-filter_complex \"{filterComplex}\"");
            altConversion.AddParameter("-map [final]"); // Map the final output from the filter graph
            var result = await altConversion.Start();

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name}\n Outputfile: {outputFilePath}\n Processing time: {result.Duration}");
            
            using (var fileStream = new FileStream(outputFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                MemoryStream videoStream = new MemoryStream();
                await fileStream.CopyToAsync(videoStream);
                return videoStream.ToArray();
            }
        }
    }
}
