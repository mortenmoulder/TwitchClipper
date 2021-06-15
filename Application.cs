using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchClipper.Services;
using TwitchClipper.Models;
using System.IO;

namespace TwitchClipper
{
    public class Application
    {
        private readonly ITwitchAPIService _twitchService;
        private readonly IYouTubeDLService _youtubeDlService;

        public Application(ITwitchAPIService twitchService, IYouTubeDLService youtubeDlService)
        {
            _twitchService = twitchService;
            _youtubeDlService = youtubeDlService;
        }

        public async Task Run(Options options)
        {
            await _youtubeDlService.CheckYouTubeDLExists();

            var clips = await _twitchService.GetClips(options.Username);

            await _youtubeDlService.DownloadClips(clips);
        }
    }
}
