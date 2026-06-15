using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;

namespace Manitux.YoutubePlugin;

public sealed class YoutubePlugin : PluginBase
{
    private const string SourceUrlPrefix = "pluginyoutube://live/";
    private const string MainPage = "https://www.youtube.com";

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.youtube.trt",
        Name = "YoutubePlugin",
        Version = "1.0.0",
        Description = "TRT 1, TRT Haber ve TRT Spor canli yayinlarini YouTube uzerinden listeler.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = MainPage,
        Favicon = "https://www.google.com/s2/favicons?domain=youtube.com&sz=64",
        Language = "tr",
        UseProxy = false,
        IsAdult = false
    };

    private static readonly List<TrtLiveChannel> Channels =
    [
        new(
            Id: "trt1",
            Title: "TRT 1",
            CategoryName: "Canli Yayinlar",
            Description: "TRT 1'in resmi YouTube canli yayini.",
            Tags: "Ulusal, Genel, TRT, Canli",
            HandleLiveUrl: "https://www.youtube.com/@trt1/live",
            Poster: "https://www.google.com/s2/favicons?domain=trt1.com.tr&sz=128",
            Backdrop: "https://www.trt1.com.tr/favicon.ico"),
        new(
            Id: "trthaber",
            Title: "TRT Haber",
            CategoryName: "Canli Yayinlar",
            Description: "TRT Haber'in resmi YouTube canli yayini.",
            Tags: "Haber, Gundem, TRT, Canli",
            HandleLiveUrl: "https://www.youtube.com/@trthaber/live",
            Poster: "https://www.google.com/s2/favicons?domain=trthaber.com&sz=128",
            Backdrop: "https://www.trthaber.com/favicon.ico"),
        new(
            Id: "trtspor",
            Title: "TRT Spor",
            CategoryName: "Canli Yayinlar",
            Description: "TRT Spor'un resmi YouTube canli yayini.",
            Tags: "Spor, TRT, Canli",
            HandleLiveUrl: "https://www.youtube.com/@trtspor/live",
            Poster: "https://www.google.com/s2/favicons?domain=trtspor.com.tr&sz=128",
            Backdrop: "https://www.trtspor.com.tr/favicon.ico")
    ];

    public override Task<List<CategoryModel>?> GetCategories()
    {
        return Task.FromResult<List<CategoryModel>?>([
            new()
            {
                Title = "Canli Yayinlar",
                Url = "pluginyoutube://category/live",
                Poster = "https://www.google.com/s2/favicons?domain=youtube.com&sz=128"
            }
        ]);
    }

    public override Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        var items = Channels
            .Where(channel => string.Equals(channel.CategoryName, category.Title, StringComparison.OrdinalIgnoreCase))
            .Skip((Math.Max(1, pageNumber) - 1) * Channels.Count)
            .Take(Channels.Count)
            .Select(ToPageItem)
            .ToList();

        return Task.FromResult<List<PageItemModel>?>(items.Count == 0 ? null : items);
    }

    public override Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<List<PageItemModel>?>(null);
        }

        var results = Channels
            .Where(channel =>
                channel.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || channel.Tags.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(ToPageItem)
            .ToList();

        return Task.FromResult<List<PageItemModel>?>(results.Count == 0 ? null : results);
    }

    public override Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem)
    {
        var channel = FindChannel(pageItem.Url);
        if (channel is null)
        {
            return Task.FromResult<MediaInfoModel?>(null);
        }

        var relatedVideos = Channels
            .Where(item => item.Id != channel.Id)
            .Select(item => new RelatedVideoModel
            {
                Title = item.Title,
                Url = BuildItemUrl(item.Id),
                Poster = item.Poster
            })
            .ToList();

        var mediaInfo = new MediaInfoModel
        {
            Title = channel.Title,
            Url = BuildItemUrl(channel.Id),
            Poster = channel.Poster,
            Backdrop = channel.Backdrop,
            Description = channel.Description,
            Tags = channel.Tags,
            Country = "TR",
            VideoSources =
            [
                new()
                {
                    Name = "YouTube Canli",
                    Url = BuildSourceUrl(channel.Id),
                    Referer = MainUrl
                }
            ],
            RelatedVideos = relatedVideos.Count == 0 ? null : relatedVideos
        };

        return Task.FromResult<MediaInfoModel?>(mediaInfo);
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        var channel = FindChannel(videoSource.Url);
        if (channel is null)
        {
            return await ExtractAsync(videoSource, videoSource.Referer ?? MainUrl);
        }

        var watchUrl = await ResolveLiveWatchUrl(channel);
        if (string.IsNullOrWhiteSpace(watchUrl))
        {
            Log(LogLevel.Warning, $"YouTube live watch url could not be resolved for {channel.Title}.");
            return null;
        }

        videoSource.Name = channel.Title;
        videoSource.Url = watchUrl;
        videoSource.Referer = channel.HandleLiveUrl;

        return await ExtractAsync(videoSource, MainUrl);
    }

    private string MainUrl => string.IsNullOrWhiteSpace(Config.MainUrl)
        ? MainPage
        : Config.MainUrl.TrimEnd('/');

    private async Task<string?> ResolveLiveWatchUrl(TrtLiveChannel channel)
    {
        try
        {
            var html = await HttpGet(channel.HandleLiveUrl, referer: MainUrl, headers: GetHeaders());
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var videoId = ExtractVideoId(html);
            return string.IsNullOrWhiteSpace(videoId) ? null : BuildYoutubeWatchUrl(videoId);
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private static string? ExtractVideoId(string html)
    {
        var watchEndpointMatch = Regex.Match(
            html,
            @"""watchEndpoint""\s*:\s*\{[^{}]*""videoId""\s*:\s*""(?<id>[A-Za-z0-9_-]{11})""",
            RegexOptions.IgnoreCase);

        if (watchEndpointMatch.Success)
        {
            return watchEndpointMatch.Groups["id"].Value;
        }

        var canonicalMatch = Regex.Match(
            html,
            @"<link\s+rel=""canonical""\s+href=""https://www\.youtube\.com/watch\?v=(?<id>[A-Za-z0-9_-]{11})""",
            RegexOptions.IgnoreCase);

        if (canonicalMatch.Success)
        {
            return canonicalMatch.Groups["id"].Value;
        }

        var videoDetailsMatch = Regex.Match(
            html,
            @"""videoDetails""\s*:\s*\{.*?""videoId""\s*:\s*""(?<id>[A-Za-z0-9_-]{11})""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return videoDetailsMatch.Success ? videoDetailsMatch.Groups["id"].Value : null;
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Accept-Language"] = "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private static PageItemModel ToPageItem(TrtLiveChannel channel)
    {
        return new PageItemModel
        {
            CategoryName = channel.CategoryName,
            Title = channel.Title,
            Url = BuildItemUrl(channel.Id),
            Poster = channel.Poster,
            Year = "Live"
        };
    }

    private static TrtLiveChannel? FindChannel(string? urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return null;
        }

        var id = urlOrId.Trim();
        if (id.StartsWith(SourceUrlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            id = id[SourceUrlPrefix.Length..];
        }
        else if (id.StartsWith("pluginyoutube://channel/", StringComparison.OrdinalIgnoreCase))
        {
            id = id["pluginyoutube://channel/".Length..];
        }

        return Channels.FirstOrDefault(channel => string.Equals(channel.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildItemUrl(string id) => $"pluginyoutube://channel/{id}";

    private static string BuildSourceUrl(string id) => $"{SourceUrlPrefix}{id}";

    private static string BuildYoutubeWatchUrl(string videoId) => $"https://www.youtube.com/watch?v={videoId}";

    private sealed record TrtLiveChannel(
        string Id,
        string Title,
        string CategoryName,
        string Description,
        string Tags,
        string HandleLiveUrl,
        string Poster,
        string Backdrop);
}
