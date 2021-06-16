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

        public Application(ITwitchAPIService twitchService, IYouTubeDLService youtubeDlService, IConfigurationService configService)
        {
            _twitchService = twitchService;
            _youtubeDlService = youtubeDlService;
            _configService = configService;
        }

        public async Task Run(Options options)
        {
            await _youtubeDlService.CheckYouTubeDLExists();

            await _twitchService.EnsureAuthTokenSet();

            var userId = await _twitchService.GetBroadcasterId(options.Username);

            var clips = await _twitchService.GetClips(userId);

            await _youtubeDlService.DownloadClips(clips);
        }
    }
}
