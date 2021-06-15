using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Services
{
    public interface IHostService
    {
        Task<string> GetYouTubeDlExecutablePath();
        Task<string> GetYouTubeDlDownloadUrl();
        Task<OSPlatform> GetOSPlatform();
        Task CreateDirectoryIfNotExists(string path);
    }

    public class HostService : IHostService
    {
        private readonly IConfigurationService _service;

        public HostService(IConfigurationService service)
        {
            _service = service;
        }

        public async Task<string> GetYouTubeDlExecutablePath()
        {
            var os = await GetOSPlatform();

            return Path.Combine(Directory.GetCurrentDirectory(), await _service.GetConfigurationValue<string>($"YouTubeDL:{os}:FileName"));
        }

        public async Task<string> GetYouTubeDlDownloadUrl()
        {
            var os = await GetOSPlatform();

            return await Task.Run(() => _service.GetConfigurationValue<string>($"YouTubeDL:{os}:Download"));
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
            if(!Directory.Exists(path))
            {
                await Task.Run(() => Directory.CreateDirectory(path));
            }
        }
    }
}
