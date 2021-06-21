using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchClipper.Helpers;
using TwitchClipper.Models;

namespace TwitchClipper.Services
{
    public interface IHostService
    {
        Task<string> GetYouTubeDlExecutablePath();
        Task<string> GetYouTubeDlDownloadUrl();
        Task<OSPlatform> GetOSPlatform();
        Task CreateDirectoryIfNotExists(string path);
        Task<string> ConvertCustomPathExpressionToSavePath(TwitchClipModel model);
    }

    public class HostService : IHostService
    {
        private readonly IConfigurationService _configService;

        public HostService(IConfigurationService configService)
        {
            _configService = configService;
        }

        public async Task<string> GetYouTubeDlExecutablePath()
        {
            var os = await GetOSPlatform();

            return Path.Combine(Directory.GetCurrentDirectory(), await _configService.GetConfigurationValue<string>($"YouTubeDL:{os}:FileName"));
        }

        public async Task<string> GetYouTubeDlDownloadUrl()
        {
            var os = await GetOSPlatform();

            return await Task.Run(() => _configService.GetConfigurationValue<string>($"YouTubeDL:{os}:Download"));
        }

        public async Task<OSPlatform> GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await Task.Run(() => OSPlatform.Windows);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await Task.Run(() => OSPlatform.Linux);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return await Task.Run(() => OSPlatform.OSX);
            }

            throw new Exception("Seems like your operating system is not supported. Please create a ticket");
        }

        public async Task CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                await Task.Run(() => Directory.CreateDirectory(path));
            }
        }

        public async Task<string> ConvertCustomPathExpressionToSavePath(TwitchClipModel model)
        {
            var path = await _configService.GetConfigurationValue<string>("Download:SavePathExpression");
            var locale = await _configService.GetConfigurationValue<string>("Download:Locale");

            CultureInfo culture = new CultureInfo("en-US");

            try
            {
                culture = new CultureInfo(locale);
            }
            catch (Exception)
            {
                await ErrorHelper.LogAndExit("Is your locale supported? Check https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c and scroll down. This is case sensitive.");
            }

            var illegalCharacters = new List<string>();

            if (Regex.Matches(path, "{").Count != Regex.Matches(path, "}").Count)
            {
                await ErrorHelper.LogAndExit("Seems like there is an unequal amount of { and } (they should be the same amount) in the custom path you wrote. Check https://github.com/mortenmoulder/TwitchClipper/wiki/Custom-save-expressions#requirements");
            }

            if (!path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                await ErrorHelper.LogAndExit("Your custom path does not end with .mp4. Check https://github.com/mortenmoulder/TwitchClipper/wiki/Custom-save-expressions#requirements");
            }

            var replace = path
                .Replace("{id", "{0")
                .Replace("{broadcaster_name", "{1")
                .Replace("{broadcaster_id", "{2")
                .Replace("{game_id", "{3")
                .Replace("{title", "{4")
                .Replace("{yyyy", "{5:yyyy").Replace("{yyy", "{5:yyy").Replace("{yy", "{5:yy").Replace("{y", "{5:%y")
                .Replace("{MMMM", "{5:MMMM").Replace("{MMM", "{5:MMM").Replace("{MM", "{5:MM").Replace("{M", "{5:%M")
                .Replace("{dddd", "{5:dddd").Replace("{ddd", "{5:ddd").Replace("{dd", "{5:dd").Replace("{d", "{5:%d")
                .Replace("{HH", "{5:HH").Replace("{H", "{5:%H").Replace("{hh", "{5:hh")
                .Replace("{mm", "{5:mm").Replace("{m", "{5:%m")
                .Replace("{ss", "{5:ss").Replace("{s", "{5:%s")
                .Replace("{tt", "{5:tt").Replace("{t", "{5:t")
                ;

            path = string.Format(culture, replace, model.Id, model.BroadcasterName, model.BroadcasterId, model.GameId, model.Title, model.CreatedAt);

            if (await GetOSPlatform() == OSPlatform.Windows)
            {
                illegalCharacters = new List<string>()
                {
                    "<", ">", ":", "\"", "/", "|", "?", "*", "{", "}"
                };

                path = path.Replace("/", @"\");
            }
            else
            {
                illegalCharacters = new List<string>()
                {
                    "{", "}"
                };

                path = path.Replace(@"\", "/");
            }

            foreach (var character in illegalCharacters)
            {
                path = path.Replace(character, "");
            }

            return path;
        }
    }
}
