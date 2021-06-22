using CommandLine;

namespace TwitchClipper.Models
{
    public class Options
    {
        [Option('u', "username", Required = false, HelpText = "Twitch username")]
        public string Username { get; set; }
        [Option("from", Required = false, HelpText = "Date from (e.g 2021-05-15) inclusive")]
        public string DateFrom { get; set; }
        [Option("to", Required = false, HelpText = "Date to (e.g 2021-05-22) inclusive")]
        public string DateTo { get; set; }
        [Option("update", Required = false, HelpText = "Check for update and update")]
        public bool Update { get; set; }
    }
}
