using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Models
{
    public class TwitchAuthRequest
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope => "";
        public string GrantType => "client_credentials";
    }
}
