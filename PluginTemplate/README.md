[English](README_en.md)

# PluginTemplate

`PluginTemplate`, Manitux eklentisi geliştirmek isteyenler için başlangıç projesidir. Örnek kategoriler, liste elemanları, medya detayları, bölümler ve video kaynakları içerir.

## Derleme

Depo kökünden şu komutu çalıştırın:

```bash
dotnet build PluginTemplate/PluginTemplate.csproj -c Release
```

Derleme çıktısı şu konumda oluşur:

```text
PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

## HTTP ve HTML yardımcıları

Plugin sınıfınız `PluginBase` sınıfından türediği için `HttpGet`, `HtmlParse`, `FixUrl`, `CleanString` ve `IsValidUrlFormat` metodlarını doğrudan kullanabilirsiniz. Bu metodlar Manitux.Core içindeki `HttpHelper` ve `HtmlHelper` sınıflarından gelir.

### HttpGet

`HttpGet` sayfa HTML'i, JSON yanıtı veya metin tabanlı API çıktısı almak için kullanılır. Boş URL için `null` döndürür; istek hata verirse hatayı loglar ve yine `null` döndürür. Bu yüzden sonuçları parse etmeden önce `string.IsNullOrWhiteSpace` kontrolü yapın.

Temel kullanım:

```csharp
var html = await HttpGet(category.Url, referer: Config.MainUrl);
if (string.IsNullOrWhiteSpace(html)) return null;
```

Özel header ve proxy kullanımı:

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

Başlıca parametreler:

- `referer`: hedef sitenin beklediği referer değerini gönderir. Verilmezse normal HTTP akışında URL referer olarak kullanılır.
- `proxyUrl`: proxy gerekiyorsa HTTP isteğini bu proxy üzerinden geçirir.
- `headers`: varsayılan header'ların yerine sizin verdiğiniz header listesini kullanır.
- `identifier`: değer verilirse TlsClient akışı kullanılır.
- `useCookie`: TlsClient isteği için özel cookie jar açar.
- `followRedirects`: yönlendirmeleri takip edip etmeyeceğini belirler.
- `cookieOutput`: `useCookie` ile gelen cookie değerlerini dışarı almak için kullanılabilir.

### TlsClient kullanımı

Bazı siteler standart `HttpClient` isteklerini engelleyebilir veya tarayıcı TLS imzası bekleyebilir. Bu durumda `identifier` parametresine `TlsClientIdentifier.Chrome144` ya da `TlsClientIdentifier.Cloudscraper` gibi bir değer verin.

```csharp
using TlsClient.Core.Models.Entities;

var html = await HttpGet(
    pageItem.Url,
    referer: Config.MainUrl,
    headers: headers,
    identifier: TlsClientIdentifier.Chrome144,
    useCookie: true);
```

`Cloudscraper` genellikle Cloudflare benzeri korumalı sayfalarda, `Chrome144` ise normal tarayıcı benzeri isteklerde tercih edilebilir. `HttpGet` işletim sistemine göre uygun TlsClient yolunu seçer; plugin tarafında ayrıca client oluşturmanız gerekmez.

Cookie almak gerekiyorsa:

```csharp
var cookies = new Dictionary<string, string>();
var html = await HttpGet(
    Config.MainUrl,
    identifier: TlsClientIdentifier.Chrome144,
    useCookie: true,
    cookieOutput: cookies);
```

### HtmlParse

`HtmlParse`, HTML metnini AngleSharp `IHtmlDocument` nesnesine çevirir. Boş veya parse edilemeyen HTML için `null` dönebilir; bu yüzden dönen belgeyi kontrol edin. Belgeyi `using var` ile kullanmak yeterlidir.

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

Sık kullanılan yöntemler:

- `QuerySelector("css")`: ilk eşleşen elementi döndürür.
- `QuerySelectorAll("css")`: tüm eşleşmeleri döndürür.
- `TextContent`: elementin görünen metnini alır.
- `GetAttribute("href")`, `GetAttribute("src")`, `GetAttribute("content")`: attribute değerlerini alır.
- `CleanString`: HTML etiketi, satır sonu, tab ve `&nbsp;` gibi kalıntıları temizler.
- `FixUrl`: göreli URL, `//cdn...` URL veya ters slash içeren değerleri tam URL'ye çevirir.
- `IsValidUrlFormat`: HTTP/HTTPS URL formatını doğrular.

## Video kaynaklarını çözümleme

Manitux.Core içinde hazır extractor sınıfları bulunur. Bunlar `Manitux.Core/Extractors/` altında tanımlıdır ve Dailymotion, YouTube, Okru, MailRu, Voe, VidMoly, StreamWish, Filemoon, MixDrop, StreamTape, Supervideo, Uqload, DoodStream ve benzeri servisleri destekler.

Bir `VideoSourceModel.Url` bu extractor sınıflarından birinin `MainUrl` veya `SupportedDomains` değeriyle eşleşirse, plugin içinde doğrudan `ExtractAsync` metodunu kullanabilirsiniz. `PluginBase.ExtractAsync`, URL için uygun extractor'ı `ExtractorManager.GetExtractorByUrl` ile bulur; eşleşme varsa ilgili extractor'ın `ExtractAsync(videoSource, referer)` metodunu çalıştırır, eşleşme yoksa mevcut `VideoSourceModel` değerini geri döndürür.

Örnek:

```csharp
public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
{
    return ExtractAsync(videoSource, Config.MainUrl);
}
```

Extractor sonucu `Url`, `Referer`, `Headers` ve `Subtitles` alanlarını güncelleyebilir. Bu yüzden desteklenen servislerde embed veya paylaşım URL'sini elle parçalamak yerine önce hazır extractor akışını kullanın; yalnızca eşleşmeyen kaynaklar için özel çözümleme kodu yazın.

## Yayınlama

Bu repo artık yayın çıktılarını `main` dalındaki `builds/` dizininde tutar. Template eklentiyi yayınlamak için derlenmiş DLL'i `builds/` altına kopyalayın ve `builds/plugins.json` içine aynı DLL için bir kayıt ekleyin.

Örnek DLL kaydı:

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
      "description": "Başlangıç Manitux eklentisi.",
      "language": "tr",
      "iconUrl": "https://www.google.com/s2/favicons?domain=example.com&sz=64",
      "isAdult": false,
      "tvTypes": ["Movie", "TvSeries"]
    }
  ]
}
```

`PluginTemplate.cs` içindeki örnek verileri gerçek kategori, listeleme, medya bilgisi ve video kaynak çözümleme mantığıyla değiştirin. Kod tarafındaki `PluginManifest.Id` değeri, `builds/plugins.json` içindeki `internalName` değeriyle aynı olmalıdır.
