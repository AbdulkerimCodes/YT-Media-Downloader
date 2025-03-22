using System;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using NAudio.Lame;
using NAudio.Wave;

class Program
{
    static async Task Main()
    {
        Console.Write("Oynatma listesi veya video URL'sini girin: ");
        string url = Console.ReadLine();

        Console.Write("Ne indirmek istiyorsunuz? (1: Video, 2: Müzik): ");
        string choice = Console.ReadLine();

        var youtube = new YoutubeClient();

        if (url.Contains("playlist"))
        {
            await foreach (var playlistVideo in youtube.Playlists.GetVideosAsync(url))
            {
                var video = await youtube.Videos.GetAsync(playlistVideo.Id);
                if (choice == "1")
                    await DownloadVideo(video);
                else if (choice == "2")
                    await DownloadAndConvertToMp3(video);
                else
                    Console.WriteLine("Geçersiz seçim!");
            }
        }
        else
        {
            var video = await youtube.Videos.GetAsync(url);
            if (choice == "1")
                await DownloadVideo(video);
            else if (choice == "2")
                await DownloadAndConvertToMp3(video);
            else
                Console.WriteLine("Geçersiz seçim!");
        }
    }

    static async Task DownloadVideo(Video video)
    {
        var youtube = new YoutubeClient();
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var muxedStreams = streamManifest.GetMuxedStreams();

        if (!muxedStreams.Any())
        {
            Console.WriteLine($"{video.Title} için uygun video akışı bulunamadı.");
            return;
        }

        var videoStreamInfo = muxedStreams.GetWithHighestVideoQuality();
        string outputFilePath = Path.Combine(Environment.CurrentDirectory, $"{SanitizeFileName(video.Title)}.mp4");

        Console.WriteLine($"{video.Title} indiriliyor...");
        await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, outputFilePath);

        Console.WriteLine($"Video olarak kaydedildi: {outputFilePath}");
    }

    static async Task DownloadAndConvertToMp3(Video video)
    {
        var youtube = new YoutubeClient();
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var audioStreams = streamManifest.GetAudioOnlyStreams();

        if (!audioStreams.Any())
        {
            Console.WriteLine($"{video.Title} için uygun ses akışı bulunamadı.");
            return;
        }

        var audioStreamInfo = audioStreams.GetWithHighestBitrate();
        string tempFilePath = Path.Combine(Path.GetTempPath(), video.Id + ".mp4");
        string outputFilePath = Path.Combine(Environment.CurrentDirectory, $"{SanitizeFileName(video.Title)}.mp3");

        Console.WriteLine($"{video.Title} indiriliyor...");
        await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempFilePath);

        ConvertToMp3(tempFilePath, outputFilePath);
        File.Delete(tempFilePath);
    }

    static void ConvertToMp3(string inputFile, string outputFile)
    {
        using (var reader = new MediaFoundationReader(inputFile))
        using (var writer = new LameMP3FileWriter(outputFile, reader.WaveFormat, LAMEPreset.STANDARD))
        {
            reader.CopyTo(writer);
        }
        Console.WriteLine($"MP3 olarak kaydedildi: {outputFile}");
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}