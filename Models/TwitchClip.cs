using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Models
{
    public class TwitchClipResponseModel
    {
        public List<TwitchClipModel> Clips { get; set; }
        [JsonPropertyAttribute("_cursor")]
        public string Cursor { get; set; }
    }

    public class TwitchClipModel
    {
        public string Slug { get; set; }
        public string TrackingId { get; set; }
        public string Url { get; set; }
        public string EmbedUrl { get; set; }
        public string EmbedHtml { get; set; }
        public TwitchBroadcaster Broadcaster { get; set; }
        public TwitchCurator Curator { get; set; }
        public TwitchVodModel Vod { get; set; }
        public string BroadcastId { get; set; }
        public string Game { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public long Views { get; set; }
        public long Duration { get; set; }
        public DateTime CreatedAt { get; set; }
        public TwitchThumbnails Thumbnails { get; set; }
    }

    public class TwitchVodModel
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public int Offset { get; set; }
        public string PreviewImageUrl { get; set; }
    }

    public class TwitchBroadcaster
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ChannelUrl { get; set; }
        public string Logo { get; set; }
    }

    public class TwitchCurator
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ChannelUrl { get; set; }
        public string Logo { get; set; }
    }

    public class TwitchThumbnails
    {
        public string Medium { get; set; }
        public string Small { get; set; }
        public string Tiny { get; set; }
    }
}
