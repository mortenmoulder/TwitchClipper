using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.GitHub_Updater
{
    public class GitHubResponse
    {
        public string TagName { get; set; }
        public string Name { get; set; }
        public List<GitHubAsset> Assets { get; set; }
    }

    public class GitHubAsset
    {
        public string Name { get; set; }
        public string BrowserDownloadUrl { get; set; }
    }
}
