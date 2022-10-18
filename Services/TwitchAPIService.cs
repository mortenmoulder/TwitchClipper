using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
        private readonly IFilteringService _filteringService;

        public TwitchAPIService(IConfigurationService service, ITwitchConfigurationService twitchConfigService, IFilteringService filteringService)
        {
            _configService = service;
            _twitchConfigService = twitchConfigService;
            _filteringService = filteringService;
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
            var clips = new List<TwitchClipModel>();
            var asyncLock = new AsyncLock();
            var page = 0;
            var hasRunFirst = false;

            var dates = await GetTwitchWeeks();

            await LogHelper.Log($"Scraping Twitch for clips. A total of {dates.Count} requests must be sent!");

            await ParallelExtensions.ParallelForEachAsync(dates, async date =>
            {
                if (hasRunFirst == false)
                {
                    LogHelper.Index += 1;
                }

                hasRunFirst = true;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Client-ID", await _twitchConfigService.GetClientID());
                httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", await _twitchConfigService.GetAuthToken()));

                var newClips = await SendRequest(userId, date, httpClient);

                clips.AddRange(newClips);

                await LogHelper.Log($"Clips found: {clips.Count} - Page {page}/{dates.Count}", asyncLock);

                page++;
            }, dates.Count >= 10 ? 10 : dates.Count);

            LogHelper.Index += 1;

            return clips.Where(x => x is not null).ToList();
        }

        /// <summary>
        /// tried to implement some stupid retry logic.. holy shit this is bad
        /// </summary>
        private async Task<List<TwitchClipModel>> SendRequest(string userId, KeyValuePair<string, string> date, HttpClient httpClient)
        {
            var url = string.Format("https://api.twitch.tv/helix/clips?broadcaster_id={0}&first={1}&started_at={2}&ended_at={3}", userId, 100, date.Key, date.Value);

            var retries = 0;

            var json = string.Empty;

            bool success;
            HttpResponseMessage result = null;

            do
            {
                try
                {
                    result = httpClient.GetAsync(url).Result;
                }
                catch (Exception ex)
                {
                    await Task.Delay(5000);
                }

                success = result?.IsSuccessStatusCode ?? false;

                if (success)
                {
                    json = await result.Content.ReadAsStringAsync();
                }

                retries++;

                if (retries == 10)
                {
                    throw new Exception($"Tried to send request {retries} times but failed. Exiting");
                }
            } while (!success);

            var model = JsonConvert.DeserializeObject<TwitchClipResponseModel>(json);

            return model.Data;
        }

        //Dictionary<from, to>
        private async Task<Dictionary<string, string>> GetTwitchWeeks()
        {
            var dates = new List<DateTime>();
            var returnDates = new Dictionary<string, string>();

            //when twitch launched clips
            var periodStart = new DateTime(2016, 05, 26);
            //tomorrow
            var periodEnd = DateTime.Today.AddDays(1);

            var filtering = await _filteringService.GetFiltering();
            if (filtering != null)
            {
                periodStart = filtering.DateFrom.Value;
                periodEnd = filtering.DateTo.Value;
            }

            var current = periodStart;
            dates.Add(current);

            do
            {
                current = current.AddDays(7);
                dates.Add(current);
            } while (current <= periodEnd);

            foreach (var date in dates.Where(x => x < periodEnd))
            {
                var from = date;
                var to = date.AddDays(7);

                if (from.AddDays(7) > periodEnd)
                {
                    to = periodEnd;
                }

                returnDates[DateToRFC3339(from)] = DateToRFC3339(to);
            }

            return await Task.Run(() => returnDates);
        }

        private string DateToRFC3339(DateTime date)
        {
            var utc = TimeZoneInfo.ConvertTimeToUtc(date);
            return XmlConvert.ToString(utc, XmlDateTimeSerializationMode.Utc);
        }
    }
}