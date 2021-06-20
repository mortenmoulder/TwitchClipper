using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TwitchClipper.Helpers;
using TwitchClipper.Models;

namespace TwitchClipper.Services
{
    public interface ITwitchAPIService
    {
        Task<string> EnsureAuthTokenSet();
        Task<string> GetBroadcasterId(string username);
        Task<List<TwitchClipModel>> GetClips(string username);
    }

    public class TwitchAPIService : ITwitchAPIService
    {
        private readonly IConfigurationService _configService;
        private readonly ITwitchConfigurationService _twitchConfigService;
        private static object _sync = new object();

        public TwitchAPIService(IConfigurationService service, ITwitchConfigurationService twitchConfigService)
        {
            _configService = service;
            _twitchConfigService = twitchConfigService;
        }

        public async Task<string> EnsureAuthTokenSet()
        {
            if (!string.IsNullOrWhiteSpace(await _twitchConfigService.GetAuthToken()))
            {
                return await _twitchConfigService.GetAuthToken();
            }

            using (var httpClient = new HttpClient())
            {
                var url = string.Format("https://id.twitch.tv/oauth2/token?client_id={0}&client_secret={1}&grant_type={2}&scope=", await _twitchConfigService.GetClientID(), await _twitchConfigService.GetClientSecret(), "client_credentials");

                var json = await httpClient.PostAsync(url, null);

                if (!json.IsSuccessStatusCode)
                {
                    await ErrorHelper.LogAndExit("Is your ClientID and ClientSecret correct? Could not auth.");
                }

                var jsonResponse = await json.Content.ReadAsStringAsync();

                var model = JsonConvert.DeserializeObject<TwitchAuthResponse>(jsonResponse);

                //set auth token in appsettings.json
                await _configService.SetConfigurationValue("TwitchConfiguration:AuthToken", model.AccessToken);

                await LogHelper.Log("Grabbed new auth token and placed it in appsettings.json");

                return model.AccessToken;
            }
        }

        public async Task<string> GetBroadcasterId(string username)
        {
            await EnsureAuthTokenSet();
            await LogHelper.Log("Grabbing the broadcaster's ID.");

            using (var httpClient = new HttpClient())
            {
                var url = string.Format("https://api.twitch.tv/helix/users?login={0}", username);

                var clientId = await _twitchConfigService.GetClientID();
                var authToken = await _twitchConfigService.GetAuthToken();
                var bearer = string.Format("Bearer {0}", authToken);

                httpClient.DefaultRequestHeaders.Add("Client-ID", clientId);
                httpClient.DefaultRequestHeaders.Add("Authorization", bearer);

                var json = await httpClient.GetStringAsync(url);

                var model = JsonConvert.DeserializeObject<TwitchUserResponseModel>(json);

                if (!model.Data.Any())
                {
                    await ErrorHelper.LogAndExit($"No data was found for username {username}. Are you sure they exist?");
                }

                return model.Data.Single().Id;
            }
        }

        public async Task<List<TwitchClipModel>> GetClips(string userId)
        {
            await EnsureAuthTokenSet();
            var clips = new List<TwitchClipModel>();
            var asyncLock = new AsyncLock();
            var page = 1;
            var hasRunFirst = false;

            var dates = await GetTwitchWeeks();

            await LogHelper.Log($"Scraping Twitch for clips. A total of {dates.Count} requests must be sent!");

            await ParallelExtensions.ParallelForEachAsync(dates, async date =>
            {
                if(hasRunFirst == false)
                {
                    LogHelper.Index += 1;
                }

                hasRunFirst = true;

                using (var httpClient = new HttpClient())
                {
                    var url = string.Format("https://api.twitch.tv/helix/clips?broadcaster_id={0}&first={1}&started_at={2}", userId, 100, date);

                    httpClient.DefaultRequestHeaders.Add("Client-ID", await _twitchConfigService.GetClientID());
                    httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", await _twitchConfigService.GetAuthToken()));

                    var json = await httpClient.GetStringAsync(url);

                    var model = JsonConvert.DeserializeObject<TwitchClipResponseModel>(json);

                    clips.AddRange(model.Data);

                    page++;

                    await LogHelper.Log($"Clips found: {clips.Count} - Page {page}/{dates.Count}", asyncLock);
                }
            }, 10);

            LogHelper.Index += 1;

            return clips;
        }

        private async Task<List<string>> GetTwitchWeeks()
        {
            var dates = new List<string>();
            //when twitch launched clips
            var periodStart = new DateTime(2016, 05, 26);
            var periodEnd = DateTime.Today;

            var date = periodEnd;
            dates.Add(DateToRFC3339(date));

            do
            {
                date = date.AddDays(-7);
                dates.Add(DateToRFC3339(date));
            } while (periodStart <= date);

            return await Task.Run(() => dates);
        }

        private string DateToRFC3339(DateTime date)
        {
            var utc = TimeZoneInfo.ConvertTimeToUtc(date);
            return XmlConvert.ToString(utc, XmlDateTimeSerializationMode.Utc);
        }
    }
}