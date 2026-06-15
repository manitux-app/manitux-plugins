[Türkçe](README.md)

# PluginTemplate

`PluginTemplate` is a starter Manitux plugin project for plugin developers. It includes example categories, page items, media details, episodes, and video sources.

## Build

Run this command from the repository root:

```bash
dotnet build PluginTemplate/PluginTemplate.csproj -c Release
```

The compiled output is created at:

```text
PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

## HTTP and HTML Helpers

Because your plugin class inherits from `PluginBase`, you can call `HttpGet`, `HtmlParse`, `FixUrl`, `CleanString`, and `IsValidUrlFormat` directly from plugin methods. These methods come from the `HttpHelper` and `HtmlHelper` classes in Manitux.Core.

### HttpGet

Use `HttpGet` to fetch page HTML, JSON responses, or other text-based API output. It returns `null` for an empty URL; when a request fails it logs the error and also returns `null`. Check the result with `string.IsNullOrWhiteSpace` before parsing it.

Basic usage:

```csharp
var html = await HttpGet(category.Url, referer: Config.MainUrl);
if (string.IsNullOrWhiteSpace(html)) return null;
```

Custom headers and proxy usage:

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

- `referer`: sends the referer value expected by the target site. When omitted, the normal HTTP flow uses the URL itself as referer.
- `proxyUrl`: routes the HTTP request through this proxy when needed.
- `headers`: uses your header list instead of the default headers.
- `identifier`: switches the request to the TlsClient flow when a value is provided.
- `useCookie`: enables a custom cookie jar for the TlsClient request.
- `followRedirects`: controls whether redirects are followed.
- `cookieOutput`: can be used with `useCookie` to read cookies returned by the response.

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

`Cloudscraper` is usually useful for Cloudflare-like protected pages, while `Chrome144` is a good browser-like default for regular pages. `HttpGet` chooses the appropriate TlsClient path for the current operating system; your plugin does not need to create a separate client.

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

`HtmlParse` converts an HTML string into an AngleSharp `IHtmlDocument`. It can return `null` for empty or unparseable HTML, so check the returned document before using it. `using var` is enough for the document lifetime.

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

- `QuerySelector("css")`: returns the first matching element.
- `QuerySelectorAll("css")`: returns all matching elements.
- `TextContent`: reads the visible text from an element.
- `GetAttribute("href")`, `GetAttribute("src")`, `GetAttribute("content")`: read attribute values.
- `CleanString`: removes HTML tags, line breaks, tabs, and leftovers such as `&nbsp;`.
- `FixUrl`: turns relative URLs, `//cdn...` URLs, or backslash-containing values into full URLs.
- `IsValidUrlFormat`: validates the HTTP/HTTPS URL format.

## Resolving Video Sources

Manitux.Core includes ready-made extractor classes. They are defined under `Manitux.Core/Extractors/` and support services such as Dailymotion, YouTube, Okru, MailRu, Voe, VidMoly, StreamWish, Filemoon, MixDrop, StreamTape, Supervideo, Uqload, DoodStream, and others.

If a `VideoSourceModel.Url` matches the `MainUrl` or `SupportedDomains` value of one of these extractor classes, you can use the `ExtractAsync` method directly from your plugin. `PluginBase.ExtractAsync` finds the matching extractor with `ExtractorManager.GetExtractorByUrl`; when a match exists, it calls that extractor's `ExtractAsync(videoSource, referer)` method, otherwise it returns the existing `VideoSourceModel`.

Example:

```csharp
public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
{
    return ExtractAsync(videoSource, Config.MainUrl);
}
```

The extractor result may update `Url`, `Referer`, `Headers`, and `Subtitles`. For supported services, prefer the built-in extractor flow before manually parsing embed or share URLs; write custom source-resolution code only for unmatched sources.

## Publish

This repository now keeps published outputs in the `builds/` directory on the `main` branch. To publish the template plugin, copy the compiled DLL under `builds/` and add an entry for the same DLL to `builds/plugins.json`.

Example DLL entry:

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

Replace the sample data in `PluginTemplate.cs` with real category, listing, media metadata, and video source extraction logic. The `PluginManifest.Id` value in code must match the `internalName` value in `builds/plugins.json`.
