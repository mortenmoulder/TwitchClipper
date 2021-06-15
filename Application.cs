using System.Threading.Tasks;
using TwitchClipper.Models;
using TwitchClipper.Services;

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
