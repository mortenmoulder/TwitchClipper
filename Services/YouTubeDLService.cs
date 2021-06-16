using RunProcessAsTask;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TwitchClipper.Helpers;
using TwitchClipper.Models;

namespace TwitchClipper.Services
{
    public interface IYouTubeDLService
    {
        Task CheckYouTubeDLExists();
        Task DownloadClips(List<TwitchClipModel> clips);
    }

    public class YouTubeDLService : IYouTubeDLService
    {
        private readonly IHostService _hostService;
        private readonly ITwitchConfigurationService _twitchConfigurationService;

        public YouTubeDLService(IHostService hostService, ITwitchConfigurationService twitchConfigurationService)
        {
            _hostService = hostService;
            _twitchConfigurationService = twitchConfigurationService;
        }

        public async Task CheckYouTubeDLExists()
        {
            if (!File.Exists(await _hostService.GetYouTubeDlExecutablePath()))
            {
                ConsoleKey response;
                do
                {
                    Console.Clear();
                    Console.WriteLine("Seems like youtube-dl has not been found. Would you like me to download it for you? [y/n]");
                    response = Console.ReadKey(false).Key;

                    if (response != ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                    }
                } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                if (response == ConsoleKey.N)
                {
                    Environment.Exit(-1);
                }
                else if (response == ConsoleKey.Y)
                {
                    Console.Clear();
                    Console.WriteLine("Download starting");

                    using (var httpClient = new HttpClient())
                    using (var httpResponse = await httpClient.GetAsync(await _hostService.GetYouTubeDlDownloadUrl()))
                    {
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            await File.WriteAllBytesAsync(await _hostService.GetYouTubeDlExecutablePath(), await httpResponse.Content.ReadAsByteArrayAsync());

                            //set magic executable permission on Linux and OSX.. very ugly
                            if (await _hostService.GetOSPlatform() == OSPlatform.Linux || await _hostService.GetOSPlatform() == OSPlatform.OSX)
                            {
                                await ProcessEx.RunAsync("/bin/bash", $"-c \"chmod +x {await _hostService.GetYouTubeDlExecutablePath()}\"");
                            }
                        }
                    }
                }
            }
        }

        public async Task DownloadClips(List<TwitchClipModel> clips)
        {
            await CreateAllDirectoriesRequired(clips);

            var root = Directory.GetCurrentDirectory();
            var username = clips.First().BroadcasterName;

            await ParallelExtensions.ParallelForEachAsync(clips, async clip =>
            {
                var path = await _hostService.ConvertCustomPathExpressionToSavePath(clip);

                path = Path.Combine(Directory.GetCurrentDirectory().TrimEnd('\\').TrimEnd('/'), "clips", path.TrimStart('\\').TrimStart('/'));

                if (!File.Exists(path))
                {
                    Console.WriteLine("Downloading: " + path);

                    await ProcessEx.RunAsync(await _hostService.GetYouTubeDlExecutablePath(), $"{clip.Url} -o \"{path}\"");
                } else
                {
                    Console.WriteLine("Skipping existing file " + path);
                }
            }, await _twitchConfigurationService.GetDownloadThreads());
        }

        private async Task CreateAllDirectoriesRequired(List<TwitchClipModel> clips)
        {
            foreach (var clip in clips)
            {
                var savePath = await _hostService.ConvertCustomPathExpressionToSavePath(clip);
                var path = savePath.Replace(Path.GetFileName(savePath), "");
                path = path.TrimEnd('\\').TrimEnd('/');

                path = Path.Combine(Directory.GetCurrentDirectory().TrimEnd('\\').TrimEnd('/'), "clips", path.TrimStart('\\').TrimStart('/'));

                await _hostService.CreateDirectoryIfNotExists(path);
            }
        }
    }
}
