using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Models
{
    public class ArchiveModel
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Broadcaster { get; set; }
        public string CreatedBy { get; set; }
        public DateTime Created { get; set; }
        public string Title { get; set; }
        public string ClipSavePath { get; set; }
        public DateTime DownloadedAt { get; set; }
    }
}
