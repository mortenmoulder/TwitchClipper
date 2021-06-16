using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Models
{
    public class TwitchUserResponseModel
    {
        public List<TwitchUser> Data { get; set; }
    }

    public class TwitchUser
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string BroadcasterType { get; set; }
        public string Description { get; set; }
        public string ProfileImageUrl { get; set; }
        public string OfflineImageUrl { get; set; }
        public long View_count { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
