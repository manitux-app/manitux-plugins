[English](README_en.md)

# PluginTemplate

`PluginTemplate`, yeni Manitux eklentisi geliştirmek için hazırlanmış tek başına örnek projedir. Bir geliştirici bu projeyi kopyalayıp kendi eklenti adına göre düzenleyerek kategori, listeleme, arama, medya detayı, bölüm ve video kaynak akışlarını doldurabilir.

## Proje Yapısı

- `PluginTemplate.csproj`: Template eklentinin proje dosyasıdır.
- `PluginTemplate.cs`: Eklentinin manifest, varsayılan config ve zorunlu Manitux metodlarını içeren örnek sınıftır.
- `README.md`: Türkçe kullanım rehberi.
- `README_en.md`: İngilizce kullanım rehberi.

`PluginTemplate.csproj` içinde `AssemblyName` değeri `PluginTemplate`, `RootNamespace` değeri `Manitux.PluginTemplate` olarak tanımlıdır. Yeni eklenti oluştururken bu değerleri kendi eklenti adınıza göre değiştirin.

## Yeni Eklentiye Çevirme

1. `PluginTemplate/` dizinini yeni eklenti adınızla kopyalayın.
2. `.csproj` dosya adını ve içindeki `AssemblyName` değerini DLL adınızla aynı yapın.
3. `RootNamespace` değerini yeni namespace ile değiştirin.
4. `PluginTemplate.cs` dosyasındaki namespace, sınıf adı, `Manifest` ve `Config` alanlarını güncelleyin.
5. `Manifest.Id` değerini benzersiz seçin. Bu değer yayın metadata'sındaki `internalName` ile aynı olmalıdır.
6. `Config.MainUrl`, `Favicon`, `Language`, `UseProxy` ve `IsAdult` değerlerini gerçek eklentinize göre doldurun.
7. Örnek `Items`, kategori, arama, medya ve video kaynak kodlarını gerçek site/API mantığıyla değiştirin.

Örnek kimlik alanları:

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
    Language = "tr",
    UseProxy = false,
    IsAdult = false
};
```

## Derleme

Depo kökünden template projeyi derlemek için:

```bash
dotnet build PluginTemplate/PluginTemplate.csproj -c Release
```

Derleme çıktısı:

```text
PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

Eklentiyi kendi adınıza kopyaladıysanız komuttaki proje yolunu ve çıktı DLL adını kendi projenize göre değiştirin.

## Manitux Metodları

`PluginTemplate`, `PluginBase` sınıfından türediği için aşağıdaki metodları doldurur:

- `GetCategories`: Uygulamada gösterilecek ana kategorileri döndürür.
- `GetPageItems`: Seçilen kategori ve sayfa numarasına göre liste elemanlarını döndürür.
- `GetSearchResults`: Arama sorgusuna göre liste elemanlarını döndürür.
- `GetMediaInfo`: Seçilen öğenin detay bilgilerini, bölüm listesini, ilişkili içerikleri ve varsa video kaynaklarını döndürür.
- `GetVideoSources`: Seçilen video kaynağını oynatılabilir gerçek kaynağa çevirir.

Boş sonuç için boş liste yerine `null` döndürmek mevcut template davranışıyla uyumludur. Hata yakaladığınız yerlerde `Log(LogLevel.Error, ex.ToString())` kullanabilirsiniz.

## Kategori ve Listeleme

Template içindeki `GetCategories` sabit örnek kategori döndürür. Gerçek eklentide bu listeyi sabit tanımlayabilir, API'den çekebilir veya ana sayfa HTML'inden parse edebilirsiniz.

```csharp
public override Task<List<CategoryModel>?> GetCategories()
{
    return Task.FromResult<List<CategoryModel>?>([
        new()
        {
            Title = "Filmler",
            Url = $"{Config.MainUrl}/filmler",
            Poster = Config.Favicon
        }
    ]);
}
```

`GetPageItems` içinde `pageNumber` değerini dikkate alın. Sayfalama destekleyen sitelerde URL'yi bu değere göre üretin; desteklemeyen sitelerde ilk sayfa dışındaki istekler için `null` döndürebilirsiniz.

## HTTP ve HTML Yardımcıları

Plugin sınıfınız `PluginBase` sınıfından türediği için `HttpGet`, `HtmlParse`, `FixUrl`, `CleanString` ve `IsValidUrlFormat` metodlarını doğrudan kullanabilirsiniz. Bu metodlar Manitux.Core içindeki `HttpHelper` ve `HtmlHelper` sınıflarından gelir.

### HttpGet

`HttpGet` sayfa HTML'i, JSON yanıtı veya metin tabanlı API çıktısı almak için kullanılır. Boş URL için `null` döndürür; istek hata verirse hatayı loglar ve yine `null` döndürür. Parse işleminden önce sonucu kontrol edin.

```csharp
var html = await HttpGet(category.Url, referer: Config.MainUrl);
if (string.IsNullOrWhiteSpace(html)) return null;
```

Özel header ve proxy örneği:

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

- `referer`: hedef sitenin beklediği referer değerini gönderir.
- `proxyUrl`: istekleri proxy üzerinden geçirir.
- `headers`: varsayılan header yerine sizin verdiğiniz header listesini kullanır.
- `identifier`: değer verilirse TlsClient akışı kullanılır.
- `useCookie`: TlsClient isteği için özel cookie jar açar.
- `followRedirects`: yönlendirmeleri takip edip etmeyeceğini belirler.
- `cookieOutput`: `useCookie` ile gelen cookie değerlerini dışarı almak için kullanılabilir.

### TlsClient Kullanımı

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

`Chrome144` normal tarayıcı benzeri istekler için iyi bir başlangıçtır. `Cloudscraper` Cloudflare benzeri korumalı sayfalarda denenebilir. Plugin tarafında ayrıca TlsClient oluşturmanız gerekmez; `HttpGet` uygun akışı seçer.

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

`HtmlParse`, HTML metnini AngleSharp `IHtmlDocument` nesnesine çevirir. Boş veya parse edilemeyen HTML için `null` dönebilir.

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
- `TextContent`: element metnini alır.
- `GetAttribute("href")`, `GetAttribute("src")`, `GetAttribute("content")`: attribute değerlerini alır.
- `CleanString`: HTML etiketi, satır sonu, tab ve `&nbsp;` kalıntılarını temizler.
- `FixUrl`: göreli URL, `//cdn...` URL veya ters slash içeren değerleri tam URL'ye çevirir.
- `IsValidUrlFormat`: HTTP/HTTPS URL formatını doğrular.

## Medya Detayı

`GetMediaInfo`, liste elemanından detay ekranına geçerken çağrılır. Template örneği film için doğrudan `VideoSources`, dizi için `Episodes` döndürür.

Medya detayında sık kullanılan alanlar:

- `Title`, `Url`, `Poster`, `Backdrop`
- `Description`, `Tags`, `Rating`, `Year`, `Duration`
- `Actors`, `Country`
- `VideoSources`
- `Episodes`
- `RelatedVideos`

Film gibi tek parça içeriklerde `VideoSources` doldurabilirsiniz. Dizi veya sezonlu içeriklerde `Episodes` döndürüp her bölümün URL'sini ayrı tutmak daha uygundur.

## Video Kaynaklarını Çözümleme

Manitux.Core içinde hazır extractor sınıfları bulunur. Bunlar `Manitux.Core/Extractors/` altında tanımlıdır ve Dailymotion, YouTube, Okru, MailRu, Voe, VidMoly, StreamWish, Filemoon, MixDrop, StreamTape, Supervideo, Uqload, DoodStream ve benzeri servisleri destekler.

Bir `VideoSourceModel.Url` bu extractor sınıflarından birinin `MainUrl` veya `SupportedDomains` değeriyle eşleşirse, plugin içinde doğrudan `ExtractAsync` metodunu kullanabilirsiniz.

```csharp
public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
{
    return ExtractAsync(videoSource, Config.MainUrl);
}
```

`PluginBase.ExtractAsync`, uygun extractor'ı `ExtractorManager.GetExtractorByUrl` ile bulur. Eşleşme varsa ilgili extractor'ın `ExtractAsync(videoSource, referer)` metodunu çalıştırır; eşleşme yoksa mevcut `VideoSourceModel` değerini geri döndürür.

Extractor sonucu `Url`, `Referer`, `Headers` ve `Subtitles` alanlarını güncelleyebilir. Desteklenen servislerde embed veya paylaşım URL'sini elle parçalamak yerine önce hazır extractor akışını kullanın. Kaynak desteklenmiyorsa `GetVideoSources` içinde kendi çözümleme kodunuzu yazabilirsiniz.

Doğrudan oynatılabilir kaynak döndürme örneği:

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

## Yayın Metadata'sı

Manitux uygulamasına eklenen ana dosya `repo.json` dosyasıdır. Bu dosya depo adını, açıklamasını, ikonunu ve eklenti listelerinin adreslerini taşır. Uygulama önce `repo.json` dosyasını okur, ardından `pluginLists` içinde verilen `builds/plugins.json` adresinden DLL kayıtlarını alır.

Örnek `repo.json`:

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

Kendi eklenti deponuzu hazırlarken `pluginLists` içindeki URL, sizin GitHub deponuzdaki raw `builds/plugins.json` adresini göstermelidir. Kullanıcı Manitux'a `repo.json` raw adresini ekler; eklenti DLL adresleri ise `builds/plugins.json` içinden çözülür.

Template eklentiyi yayınlamak için derlenmiş DLL'i `builds/` altına koyup `builds/plugins.json` içine aynı DLL için kayıt ekleyin. `url` alanındaki dosya adı, derlenen DLL adıyla aynı olmalıdır.

Örnek kayıt:

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

Alan eşleşmeleri:

- `repo.json.pluginLists`: Manitux'un okuyacağı bir veya daha fazla `plugins.json` adresi.
- `url`: DLL'in raw GitHub adresi.
- `version`: DLL içeriği değiştiğinde artırılacak paket versiyonu.
- `apiVersion`: Manitux plugin API versiyonu.
- `plugins[].internalName`: Kod tarafındaki `Manifest.Id` ile aynı olmalıdır.
- `plugins[].name`: Kullanıcıya görünen eklenti adı.
- `plugins[].description`: Kısa açıklama.
- `plugins[].language`: Varsayılan dil kodu.
- `plugins[].iconUrl`: Eklenti ikon adresi.
- `plugins[].isAdult`: Yetişkin içerik bilgisi.
- `plugins[].tvTypes`: Desteklenen içerik tipleri.

## Kontrol Listesi

- Proje adı, namespace, class adı ve DLL adı güncellendi.
- `Manifest.Id` benzersiz ve `plugins.json` içindeki `internalName` ile aynı.
- `Config.MainUrl`, `Favicon`, `Language` ve `IsAdult` doğru.
- `GetCategories`, `GetPageItems`, `GetSearchResults`, `GetMediaInfo` ve `GetVideoSources` gerçek veriyle çalışıyor.
- HTTP isteklerinde boş sonuç ve hata durumları kontrol ediliyor.
- HTML parse ederken göreli URL'ler `FixUrl` ile tam URL'ye çevriliyor.
- Desteklenen kaynaklarda `ExtractAsync` kullanılıyor.
- Release build alındı ve DLL adı metadata'daki `url` ile eşleşiyor.
