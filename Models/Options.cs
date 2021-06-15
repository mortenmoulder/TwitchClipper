using CommandLine;

namespace TwitchClipper.Models
{
    public class Options
    {
        [Option('u', "username", Required = true, HelpText = "Twitch username")]
        public string Username { get; set; }
    }
}
