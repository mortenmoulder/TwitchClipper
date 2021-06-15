using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TwitchClipper.Models;

namespace TwitchClipper.Services
{
    public interface ITwitchAPIService
    {
        Task<List<TwitchClipModel>> GetClips(string username);
    }

    public class TwitchAPIService : ITwitchAPIService
    {
        private readonly ITwitchConfigurationService _service;

        public TwitchAPIService(ITwitchConfigurationService service)
        {
            _service = service;
        }

        public async Task<List<TwitchClipModel>> GetClips(string username)
        {
            var clips = new List<TwitchClipModel>();
            var cursor = string.Empty;

            do
            {
                using (var httpClient = new HttpClient())
                {
                    var url = string.Format("https://api.twitch.tv/kraken/clips/top?channel={0}&limit={1}&period={2}", username, 100, "all");

                    if (!string.IsNullOrWhiteSpace(cursor))
                    {
                        url += $"&cursor={cursor}";
                    }

                    httpClient.DefaultRequestHeaders.Add("Client-ID", await _service.GetClientID());
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.twitchtv.v5+json");

                    var json = await httpClient.GetStringAsync(url);

                    var model = JsonConvert.DeserializeObject<TwitchClipResponseModel>(json);

                    clips.AddRange(model.Clips);

                    Console.WriteLine($"Found {model.Clips.Count()} clips. Total: {clips.Count()}");

                    cursor = string.IsNullOrWhiteSpace(model.Cursor) ? null : model.Cursor;
                }
            } while (!string.IsNullOrWhiteSpace(cursor));

            return clips;
        }
    }
}
