using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchClipper.Models;

namespace TwitchClipper.Services
{
    public interface IFilteringService
    {
        Task<Filtering> GetFiltering();
        Task SetFiltering(Filtering filter);
    }

    public class FilteringService : IFilteringService
    {
        public Filtering Filtering { get; set; }

        public async Task<Filtering> GetFiltering()
        {
            return await Task.Run(() => Filtering);
        }

        public async Task SetFiltering(Filtering filter)
        {
            await Task.Run(() => Filtering = filter);
        }
    }
}
