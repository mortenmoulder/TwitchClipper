using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Services
{
    public interface IConfigurationService
    {
        Task<T> GetConfigurationValue<T>(string key);
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
    }
}
