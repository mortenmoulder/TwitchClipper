using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchClipper.Helpers;
using TwitchClipper.Services;

namespace TwitchClipper.GitHub_Updater
{
    public interface IGitHubUpdater
    {
        Task CheckForUpdate(bool skipPrompt);
    }

    public class GitHubUpdater : IGitHubUpdater
    {
        private readonly IHostService _hostService;

        public GitHubUpdater(IHostService hostService)
        {
            _hostService = hostService;
        }

        public async Task CheckForUpdate(bool skipPrompt)
        {
            await DeleteTempFileIfExists();

            var currentVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Version gitHubVersion;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                var json = await client.GetStringAsync("https://api.github.com/repos/mortenmoulder/twitchclipper/releases/latest");
                var response = JsonConvert.DeserializeObject<GitHubResponse>(json);
                gitHubVersion = new Version(response.TagName);
            }

            if (gitHubVersion > currentVersion)
            {
                var response = skipPrompt || await PromptForUpdate(gitHubVersion.ToString());

                if (response == true)
                {
                    await DownloadUpdate();
                }
            }
        }

        private async Task<bool> PromptForUpdate(string version)
        {
            var currentVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());

            var response = false;
            var countdown = 10;

            while (countdown > 0)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write($"There is a new version {version}. You are on {currentVersion}. Would you like to download the new update? [y/n] ({countdown}s until decline) ");

                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(false).Key;

                    if (key == ConsoleKey.Y)
                    {
                        response = true;
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

            LogHelper.Index += 1;

            return response;
        }

        private async Task DownloadUpdate()
        {
            string downloadUrl;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                var json = await client.GetStringAsync("https://api.github.com/repos/mortenmoulder/twitchclipper/releases/latest");
                var response = JsonConvert.DeserializeObject<GitHubResponse>(json);

                var platform = string.Empty;
                var os = await _hostService.GetOSPlatform();

                if (os == OSPlatform.Windows)
                {
                    platform = "win-x64";
                }
                else if (os == OSPlatform.Linux)
                {
                    platform = "linux-x64";
                }
                else if (os == OSPlatform.OSX)
                {
                    platform = "osx-x64";
                }

                var downloadUrls = response.Assets.Where(x => x.Name.Contains(platform, StringComparison.OrdinalIgnoreCase));

                if (!downloadUrls.Any())
                {
                    await LogHelper.Log($"Unable to find new release for platform {platform}. This might be a bug, because the new release hasn't been downloaded yet. Please try again later");
                    return;
                }

                downloadUrl = downloadUrls.Single().BrowserDownloadUrl;
            }

            using (var client = new HttpClient())
            using (var httpResponse = await client.GetAsync(downloadUrl))
            {
                if (httpResponse.IsSuccessStatusCode)
                {
                    using (var memoryStream = new MemoryStream(await httpResponse.Content.ReadAsByteArrayAsync()))
                    {
                        await SaveFile(memoryStream);
                    }
                }
                else
                {
                    await LogHelper.Log($"Failed to download file from {downloadUrl}. Is this a bug? Please try downloading it manually.");
                }
            }
        }

        // this method is really hacky. why?
        // move current running binary to .tmp and then save new binary to old (currently running) location
        private async Task SaveFile(MemoryStream stream)
        {
            var os = await _hostService.GetOSPlatform();
            var path = Directory.GetCurrentDirectory();

            if (os == OSPlatform.Windows)
            {
                var executablePath = Path.Combine(path, "TwitchClipper.exe");
                var temp = executablePath + ".tmp";

                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }

                File.Move(executablePath, temp);

                using (ZipArchive zip = new ZipArchive(stream))
                {
                    var executable = zip.Entries.Where(x => x.Name == "TwitchClipper.exe").Single();

                    using (var ms = new MemoryStream())
                    {
                        await executable.Open().CopyToAsync(ms);

                        await File.WriteAllBytesAsync(executablePath, ms.ToArray());
                    }
                }
            }
            else
            {
                var executablePath = Path.Combine(path, "TwitchClipper");

                var temp = executablePath + ".tmp";

                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }

                File.Move(executablePath, temp);

                using (var reader = ReaderFactory.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory && reader.Entry.Key.EndsWith("TwitchClipper"))
                        {
                            using (var ms = new MemoryStream())
                            {
                                reader.WriteEntryTo(ms);

                                await File.WriteAllBytesAsync(executablePath, ms.ToArray());
                            }
                        }
                    }
                }
            }

            await ErrorHelper.LogAndExit("Update complete! Please run the same command again");
        }

        private async Task DeleteTempFileIfExists()
        {
            var os = await _hostService.GetOSPlatform();
            var path = Directory.GetCurrentDirectory();
            string temp;

            if (os == OSPlatform.Windows)
            {
                var executablePath = Path.Combine(path, "TwitchClipper.exe");
                temp = executablePath + ".tmp";
            }
            else
            {
                var executablePath = Path.Combine(path, "TwitchClipper");
                temp = executablePath + ".tmp";
            }

            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
}
