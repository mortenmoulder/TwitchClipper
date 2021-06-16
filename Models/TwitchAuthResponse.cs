using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Models
{
    public class TwitchAuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public List<string> Scope { get; set; }
        public string TokenType { get; set; }
    }
}
