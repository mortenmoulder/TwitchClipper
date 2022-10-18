using Nito.AsyncEx;
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
    public interface IDownloaderService
    {
        Task CheckExecutableExists();
        Task DownloadClips(List<TwitchClipModel> clips);
    }

    public class DownloaderService : IDownloaderService
    {
        private readonly IHostService _hostService;
        private readonly ITwitchConfigurationService _twitchConfigurationService;
        private readonly IArchivingService _archivingService;

        public DownloaderService(IHostService hostService, ITwitchConfigurationService twitchConfigurationService, IArchivingService archivingService)
        {
            _hostService = hostService;
            _twitchConfigurationService = twitchConfigurationService;
            _archivingService = archivingService;
        }

        public async Task CheckExecutableExists()
        {
            if (!File.Exists(await _hostService.GetDownloaderExecutablePath()))
            {
                var response = true;
                var countdown = 10;

                while (countdown > 0)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.Write($"Seems like yt-dlp has not been found. Would you like me to download it for you? [y/n] ({countdown}s until auto accept) ");

                    if (Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(false).Key;

                        if (key == ConsoleKey.Y)
                        {
                            break;
                        }

                        if (key == ConsoleKey.N)
                        {
                            response = false;
                            break;
                        }
                    }

                    await Task.Delay(1000);
                    countdown--;
                }

                if (response == false)
                {
                    await ErrorHelper.LogAndExit("You decided not to download yt-dlp, therefore the application must exit. yt-dlp is a requirement.");
                }
                
                if (response == true)
                {
                    await LogHelper.Log("Download starting");

                    using (var httpClient = new HttpClient())
                    using (var httpResponse = await httpClient.GetAsync(await _hostService.GetDownloaderDownloadUrl()))
                    {
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            await File.WriteAllBytesAsync(await _hostService.GetDownloaderExecutablePath(), await httpResponse.Content.ReadAsByteArrayAsync());

                            //set magic executable permission on Linux and OSX.. very ugly
                            if (!(await _hostService.GetOSPlatform() == OSPlatform.Windows))
                            {
                                await ProcessEx.RunAsync("/bin/bash", $"-c \"chmod +x {await _hostService.GetDownloaderExecutablePath()}\"");
                            }
                        }
                    }
                }
            }
        }

        public async Task DownloadClips(List<TwitchClipModel> clips)
        {
            var root = Directory.GetCurrentDirectory();
            var username = clips.First().BroadcasterName;

            var nonExistingClips = clips.Select(async x => File.Exists(await GetPath(x))).Select(x => x.Result).Where(x => x == false).ToList();

            await LogHelper.Log($"Found a total of {clips.Count} clips and {clips.Count - nonExistingClips.Count} already exists. Downloading {nonExistingClips.Count} clips.");

            var asyncLock = new AsyncLock();

            var counter = 0;
            await ParallelExtensions.ParallelForEachAsync(clips, async clip =>
            {
                var path = await GetPath(clip);
                await _archivingService.Log(clip, path, asyncLock);

                if (!File.Exists(path))
                {
                    counter++;
                    await ProcessEx.RunAsync(await _hostService.GetDownloaderExecutablePath(), $"{clip.Url} --restrict-filenames --windows-filenames -o \"{path}\"");
                    await LogHelper.Log($"Downloading clip {counter}/{nonExistingClips.Count} using {await _twitchConfigurationService.GetDownloadThreads()} download threads", asyncLock);
                }
            }, await _twitchConfigurationService.GetDownloadThreads());

            LogHelper.Index += 1;
        }

        private async Task<string> GetPath(TwitchClipModel clip)
        {
            var path = await _hostService.ConvertCustomPathExpressionToSavePath(clip);

            path = Path.Combine(Directory.GetCurrentDirectory().TrimEnd('\\').TrimEnd('/'), "clips", path.TrimStart('\\').TrimStart('/'));

            return path;
        }
    }
}
