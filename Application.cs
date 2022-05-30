using System;
using System.Linq;
using System.Threading.Tasks;
using TwitchClipper.GitHub_Updater;
using TwitchClipper.Helpers;
using TwitchClipper.Models;
using TwitchClipper.Services;

namespace TwitchClipper
{
    public class Application
    {
        private readonly ITwitchAPIService _twitchService;
        private readonly IYouTubeDLService _youtubeDlService;
        private readonly IHostService _hostService;
        private readonly IGitHubUpdater _updater;
        private readonly IArchivingService _archivingService;

        public Application(ITwitchAPIService twitchService, IYouTubeDLService youtubeDlService, IHostService hostService, IGitHubUpdater updater, IArchivingService archivingService)
        {
            _twitchService = twitchService;
            _youtubeDlService = youtubeDlService;
            _hostService = hostService;
            _updater = updater;
            _archivingService = archivingService;
        }

        public async Task Run(Options options)
        {
            await _archivingService.LoadLogs();
            await _updater.CheckForUpdate(options.Update);

            if(string.IsNullOrWhiteSpace(options.Username))
            {
                await ErrorHelper.LogAndExit("Seems like you're missing the --username argument");
            }

            //yes, placing this here is really bad. Please don't blame me
            await TestCustomPathExpression();

            await _youtubeDlService.CheckYouTubeDLExists();

            await _twitchService.EnsureAuthTokenSet();

            //seems like writing to the file, then reading from it immediately after introduces some problems.. so here's a fix
            await Task.Delay(1000);

            var userId = await _twitchService.GetBroadcasterId(options.Username);

            var clips = await _twitchService.GetClips(userId);

            if(!clips.Any())
            {
                await ErrorHelper.LogAndExit("No clips found in the given period.");
            }

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
