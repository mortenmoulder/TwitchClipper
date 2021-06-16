using System;
using System.Threading.Tasks;
using TwitchClipper.Models;
using TwitchClipper.Services;

namespace TwitchClipper
{
    public class Application
    {
        private readonly ITwitchAPIService _twitchService;
        private readonly IYouTubeDLService _youtubeDlService;
        private readonly IConfigurationService _configService;
        private readonly IHostService _hostService;

        public Application(ITwitchAPIService twitchService, IYouTubeDLService youtubeDlService, IConfigurationService configService, IHostService hostService)
        {
            _twitchService = twitchService;
            _youtubeDlService = youtubeDlService;
            _configService = configService;
            _hostService = hostService;
        }

        public async Task Run(Options options)
        {
            //yes, placing this here is really bad. Please don't blame me
            await TestCustomPathExpression();

            await _youtubeDlService.CheckYouTubeDLExists();

            await _twitchService.EnsureAuthTokenSet();

            var userId = await _twitchService.GetBroadcasterId(options.Username);

            var clips = await _twitchService.GetClips(userId);

            await _youtubeDlService.DownloadClips(clips);
        }

        private async Task TestCustomPathExpression()
        {
            var model = new TwitchClipModel
            {
                BroadcasterId = "12345",
                BroadcasterName = "mortenmoulder",
                CreatedAt = DateTime.UtcNow,
                Id = "VeryAwesomeClip",
                GameId = "1337"
            };

            await _hostService.ConvertCustomPathExpressionToSavePath(model);
        }
    }
}
