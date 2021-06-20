using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TwitchClipper.Helpers;

namespace TwitchClipper.Services
{
    public interface IConfigurationService
    {
        Task<T> GetConfigurationValue<T>(string key);
        Task SetConfigurationValue<T>(string sectionPathKey, T value);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<T> GetConfigurationValue<T>(string key)
        {
            var val = _configuration.GetSection(key).Value;

            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(val, out int result))
                {
                    return await Task.Run(() => (T)Convert.ChangeType(result, typeof(T)));
                }

                throw new Exception($"Expected {key} to be an integer value (whole number)");
            }

            return await Task.Run(() => (T)Convert.ChangeType(val, typeof(T)));
        }

        public async Task SetConfigurationValue<T>(string sectionPathKey, T value)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                string json = await File.ReadAllTextAsync(filePath);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);

                SetValueRecursively(sectionPathKey, jsonObj, value);

                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, output);

            }
            catch (Exception ex)
            {
                await LogHelper.Log($"Error writing app settings | {ex.Message}");
            }
        }

        private static void SetValueRecursively<T>(string sectionPathKey, dynamic jsonObj, T value)
        {
            // split the string at the first ':' character
            var remainingSections = sectionPathKey.Split(":", 2);

            var currentSection = remainingSections[0];
            if (remainingSections.Length > 1)
            {
                // continue with the procress, moving down the tree
                var nextSection = remainingSections[1];
                SetValueRecursively(nextSection, jsonObj[currentSection], value);
            }
            else
            {
                // we've got to the end of the tree, set the value
                jsonObj[currentSection] = value;
            }
        }
    }
}
