using System.Threading.Tasks;

namespace TwitchClipper.Services
{
    public interface ITwitchConfigurationService
    {
        Task<string> GetClientID();
        Task<string> GetClientSecret();
        Task<int> GetDownloadThreads();
    }

    public class TwitchConfigurationService : ITwitchConfigurationService
    {
        private readonly IConfigurationService _service;

        public TwitchConfigurationService(IConfigurationService service)
        {
            _service = service;
        }

        public async Task<string> GetClientID() => await _service.GetConfigurationValue<string>("TwitchConfiguration:ClientID");
        public async Task<string> GetClientSecret() => await _service.GetConfigurationValue<string>("TwitchConfiguration:ClientSecret");
        public async Task<int> GetDownloadThreads() => await _service.GetConfigurationValue<int>("TwitchConfiguration:DownloadThreads");
    }
}
