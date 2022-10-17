﻿using Nito.AsyncEx;
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
                ConsoleKey response;
                do
                {
                    await LogHelper.Log("Seems like yt-dlp has not been found. Would you like me to download it for you? [y/n]");
                    response = Console.ReadKey(false).Key;

                    if (response != ConsoleKey.Enter)
                    {
                        await LogHelper.Log("");
                    }
                } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                if (response == ConsoleKey.N)
                {
                    await ErrorHelper.LogAndExit("You decided not to download yt-dlp, therefore the application must exit. yt-dlp is a requirement.");
                }
                else if (response == ConsoleKey.Y)
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
            await CreateAllDirectoriesRequired(clips);

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
                    await ProcessEx.RunAsync(await _hostService.GetDownloaderExecutablePath(), $"{clip.Url} -o \"{path}\"");
                    await LogHelper.Log($"Downloading clip {counter}/{nonExistingClips.Count} using {await _twitchConfigurationService.GetDownloadThreads()} download threads", asyncLock);
                }
            }, await _twitchConfigurationService.GetDownloadThreads());

            LogHelper.Index += 1;
        }

        private async Task CreateAllDirectoriesRequired(List<TwitchClipModel> clips)
        {
            await LogHelper.Log("Creating directories. This might take a while.");

            foreach (var clip in clips)
            {
                var savePath = await _hostService.ConvertCustomPathExpressionToSavePath(clip);
                var path = savePath.Replace(Path.GetFileName(savePath), "");
                path = path.TrimEnd('\\').TrimEnd('/');

                path = Path.Combine(Directory.GetCurrentDirectory().TrimEnd('\\').TrimEnd('/'), "clips", path.TrimStart('\\').TrimStart('/'));

                await _hostService.CreateDirectoryIfNotExists(path);
            }
        }

        private async Task<string> GetPath(TwitchClipModel clip)
        {
            var path = await _hostService.ConvertCustomPathExpressionToSavePath(clip);

            path = Path.Combine(Directory.GetCurrentDirectory().TrimEnd('\\').TrimEnd('/'), "clips", path.TrimStart('\\').TrimStart('/'));

            return path;
        }
    }
}