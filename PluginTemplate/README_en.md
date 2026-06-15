[Türkçe](README.md)

# PluginTemplate

`PluginTemplate` is a standalone starter project for building a new Manitux plugin. A developer can copy this project, rename it for a real plugin, and fill in category, listing, search, media detail, episode, and video source flows.

## Project Structure

- `PluginTemplate.csproj`: Project file for the template plugin.
- `PluginTemplate.cs`: Sample plugin class containing the manifest, default config, and required Manitux methods.
- `README.md`: Turkish guide.
- `README_en.md`: English guide.

`PluginTemplate.csproj` defines `AssemblyName` as `PluginTemplate` and `RootNamespace` as `Manitux.PluginTemplate`. Change these values when creating your own plugin.

## Turning It Into a New Plugin

1. Copy the `PluginTemplate/` directory with your new plugin name.
2. Rename the `.csproj` file and set its `AssemblyName` to the same DLL name.
3. Change `RootNamespace` to the new namespace.
4. Update the namespace, class name, `Manifest`, and `Config` fields in `PluginTemplate.cs`.
5. Choose a unique `Manifest.Id`. This value must match `internalName` in the publish metadata.
6. Fill `Config.MainUrl`, `Favicon`, `Language`, `UseProxy`, and `IsAdult` for your real plugin.
7. Replace the sample `Items`, category, search, media, and video source code with real site/API logic.

Example identity fields:

```csharp
public override PluginManifest Manifest { get; } = new()
{
    Id = "plugin.example",
    Name = "Example",
    Version = "1.0.0",
    Description = "Example Manitux plugin.",
    Author = "Your Name"
};

public override PluginConfig Config { get; set; } = new()
{
    MainUrl = "https://example.com",
    Favicon = "https://www.google.com/s2/favicons?domain=example.com&sz=64",
    Language = "en",
    UseProxy = false,
    IsAdult = false
};
```

## Build

From the repository root, build the template project with:

```bash
dotnet build PluginTemplate/PluginTemplate.csproj -c Release
```

Build output:

```text
PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

If you copied the plugin under a different name, replace the project path and output DLL name with your own project values.

## Manitux Methods

`PluginTemplate` inherits from `PluginBase` and implements these methods:

- `GetCategories`: Returns the main categories shown in the app.
- `GetPageItems`: Returns listing items for the selected category and page number.
- `GetSearchResults`: Returns listing items for a search query.
- `GetMediaInfo`: Returns detail metadata, episodes, related videos, and video sources for the selected item.
- `GetVideoSources`: Resolves the selected video source into a playable source.

Returning `null` for an empty result is consistent with the current template behavior. Where you catch errors, you can use `Log(LogLevel.Error, ex.ToString())`.

## Categories and Listings

The template `GetCategories` method returns fixed sample categories. In a real plugin, the list can be static, fetched from an API, or parsed from the main page HTML.

```csharp
public override Task<List<CategoryModel>?> GetCategories()
{
    return Task.FromResult<List<CategoryModel>?>([
        new()
        {
            Title = "Movies",
            Url = $"{Config.MainUrl}/movies",
            Poster = Config.Favicon
        }
    ]);
}
```

In `GetPageItems`, respect the `pageNumber` value. For sites with pagination, generate the URL from that value; for sites without pagination, returning `null` after the first page is fine.

## HTTP and HTML Helpers

Because your plugin class inherits from `PluginBase`, you can call `HttpGet`, `HtmlParse`, `FixUrl`, `CleanString`, and `IsValidUrlFormat` directly from plugin methods. These methods come from the `HttpHelper` and `HtmlHelper` classes in Manitux.Core.

### HttpGet

Use `HttpGet` to fetch page HTML, JSON responses, or other text-based API output. It returns `null` for an empty URL; when a request fails it logs the error and also returns `null`. Check the result before parsing it.

```csharp
var html = await HttpGet(category.Url, referer: Config.MainUrl);
if (string.IsNullOrWhiteSpace(html)) return null;
```

Custom headers and proxy example:

```csharp
var headers = new Dictionary<string, string>
{
    ["User-Agent"] = "Mozilla/5.0",
    ["Accept"] = "text/html,application/xhtml+xml"
};
var proxyUrl = Config.UseProxy ? "http://127.0.0.1:8080" : null;

var html = await HttpGet(
    pageItem.Url,
    referer: Config.MainUrl,
    proxyUrl: proxyUrl,
    headers: headers);
```

Main parameters:

- `referer`: Sends the referer value expected by the target site.
- `proxyUrl`: Routes requests through a proxy.
- `headers`: Uses your header list instead of the default headers.
- `identifier`: Switches the request to the TlsClient flow when a value is provided.
- `useCookie`: Enables a custom cookie jar for the TlsClient request.
- `followRedirects`: Controls whether redirects are followed.
- `cookieOutput`: Can be used with `useCookie` to read cookies returned by the response.

### TlsClient Usage

Some sites may block standard `HttpClient` requests or require a browser-like TLS fingerprint. In that case, pass a value such as `TlsClientIdentifier.Chrome144` or `TlsClientIdentifier.Cloudscraper` to the `identifier` parameter.

```csharp
using TlsClient.Core.Models.Entities;

var html = await HttpGet(
    pageItem.Url,
    referer: Config.MainUrl,
    headers: headers,
    identifier: TlsClientIdentifier.Chrome144,
    useCookie: true);
```

`Chrome144` is a good starting point for regular browser-like requests. `Cloudscraper` can be tried for Cloudflare-like protected pages. Your plugin does not need to create a separate TlsClient; `HttpGet` selects the appropriate flow.

To collect cookies:

```csharp
var cookies = new Dictionary<string, string>();
var html = await HttpGet(
    Config.MainUrl,
    identifier: TlsClientIdentifier.Chrome144,
    useCookie: true,
    cookieOutput: cookies);
```

### HtmlParse

`HtmlParse` converts an HTML string into an AngleSharp `IHtmlDocument`. It can return `null` for empty or unparseable HTML.

```csharp
var html = await HttpGet(category.Url, referer: Config.MainUrl);
if (string.IsNullOrWhiteSpace(html)) return null;

using var document = await HtmlParse(html);
if (document is null) return null;

var items = document
    .QuerySelectorAll(".movie-card")
    .Select(card => new PageItemModel
    {
        Title = CleanString(card.QuerySelector(".title")?.TextContent ?? ""),
        Url = FixUrl(card.QuerySelector("a")?.GetAttribute("href") ?? "", Config.MainUrl),
        Poster = FixUrl(card.QuerySelector("img")?.GetAttribute("src") ?? "", Config.MainUrl),
        CategoryName = category.Title
    })
    .Where(item => !string.IsNullOrWhiteSpace(item.Title) && IsValidUrlFormat(item.Url))
    .ToList();
```

Common methods:

- `QuerySelector("css")`: Returns the first matching element.
- `QuerySelectorAll("css")`: Returns all matching elements.
- `TextContent`: Reads element text.
- `GetAttribute("href")`, `GetAttribute("src")`, `GetAttribute("content")`: Read attribute values.
- `CleanString`: Removes HTML tags, line breaks, tabs, and leftovers such as `&nbsp;`.
- `FixUrl`: Turns relative URLs, `//cdn...` URLs, or backslash-containing values into full URLs.
- `IsValidUrlFormat`: Validates the HTTP/HTTPS URL format.

## Media Details

`GetMediaInfo` is called when the app opens a listing item. The template returns direct `VideoSources` for movie-like items and `Episodes` for series-like items.

Common media detail fields:

- `Title`, `Url`, `Poster`, `Backdrop`
- `Description`, `Tags`, `Rating`, `Year`, `Duration`
- `Actors`, `Country`
- `VideoSources`
- `Episodes`
- `RelatedVideos`

For single-item content such as movies, you can fill `VideoSources`. For series or season-based content, return `Episodes` and keep each episode URL separate.

## Resolving Video Sources

Manitux.Core includes ready-made extractor classes. They are defined under `Manitux.Core/Extractors/` and support services such as Dailymotion, YouTube, Okru, MailRu, Voe, VidMoly, StreamWish, Filemoon, MixDrop, StreamTape, Supervideo, Uqload, DoodStream, and others.

If a `VideoSourceModel.Url` matches the `MainUrl` or `SupportedDomains` value of one of these extractor classes, you can use `ExtractAsync` directly from your plugin.

```csharp
public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
{
    return ExtractAsync(videoSource, Config.MainUrl);
}
```

`PluginBase.ExtractAsync` finds the matching extractor with `ExtractorManager.GetExtractorByUrl`. When a match exists, it calls that extractor's `ExtractAsync(videoSource, referer)` method; otherwise it returns the existing `VideoSourceModel`.

The extractor result may update `Url`, `Referer`, `Headers`, and `Subtitles`. For supported services, prefer the built-in extractor flow before manually parsing embed or share URLs. If the source is not supported, write custom resolution code inside `GetVideoSources`.

Example direct playable source:

```csharp
return Task.FromResult<VideoSourceModel?>(new VideoSourceModel
{
    Name = "Main Source",
    Url = "https://example.com/video/master.m3u8",
    Referer = Config.MainUrl,
    Headers =
    [
        new HeaderModel { Name = "User-Agent", Value = "Mozilla/5.0" }
    ]
});
```

## Publish Metadata

The main file added to the Manitux app is `repo.json`. It contains the repository name, description, icon, and the URLs of the plugin lists. The app reads `repo.json` first, then loads DLL entries from the `builds/plugins.json` URL listed in `pluginLists`.

Example `repo.json`:

```json
{
  "name": "Manitux Plugin Repository",
  "description": "This repository contains a collection of plugins for Manitux",
  "iconUrl": "https://www.google.com/s2/favicons?domain=github.com&sz=64",
  "manifestVersion": 1,
  "pluginLists": [
    "https://raw.githubusercontent.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY/main/builds/plugins.json"
  ]
}
```

When preparing your own plugin repository, the URL inside `pluginLists` must point to the raw `builds/plugins.json` file in your GitHub repository. Users add the raw `repo.json` URL to Manitux; plugin DLL URLs are resolved from `builds/plugins.json`.

To publish the template plugin, place the compiled DLL under `builds/` and add an entry for that same DLL to `builds/plugins.json`. The file name in `url` must match the compiled DLL name.

Example entry:

```json
{
  "url": "https://raw.githubusercontent.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY/main/builds/PluginTemplate.dll",
  "status": 1,
  "version": 1,
  "apiVersion": 1,
  "authors": ["Your Name"],
  "repositoryUrl": "https://github.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY",
  "plugins": [
    {
      "name": "Plugin Template",
      "internalName": "plugin.template",
      "description": "A starter Manitux plugin.",
      "language": "en",
      "iconUrl": "https://www.google.com/s2/favicons?domain=example.com&sz=64",
      "isAdult": false,
      "tvTypes": ["Movie", "TvSeries"]
    }
  ]
}
```

Field mapping:

- `repo.json.pluginLists`: One or more `plugins.json` URLs that Manitux will read.
- `url`: Raw GitHub URL of the DLL.
- `version`: Package version to increment when the DLL contents change.
- `apiVersion`: Manitux plugin API version.
- `plugins[].internalName`: Must match the code-side `Manifest.Id`.
- `plugins[].name`: Plugin name shown to the user.
- `plugins[].description`: Short description.
- `plugins[].language`: Default language code.
- `plugins[].iconUrl`: Plugin icon URL.
- `plugins[].isAdult`: Adult content flag.
- `plugins[].tvTypes`: Supported content types.

## Checklist

- Project name, namespace, class name, and DLL name are updated.
- `Manifest.Id` is unique and matches `internalName` in `plugins.json`.
- `Config.MainUrl`, `Favicon`, `Language`, and `IsAdult` are correct.
- `GetCategories`, `GetPageItems`, `GetSearchResults`, `GetMediaInfo`, and `GetVideoSources` work with real data.
- Empty HTTP responses and error cases are handled.
- Relative URLs from HTML parsing are converted with `FixUrl`.
- `ExtractAsync` is used for supported video sources.
- Release build is created and the DLL name matches the metadata `url`.
