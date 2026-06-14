[English](README_en.md)  

# Manitux Eklenti Deposu

Bu depo, [Manitux](https://github.com/manitux-app/manitux) için bir eklenti koleksiyonu içermektedir.

Repo.json dosyasının ham URL'sini Manitux uygulama ayarları sayfasına ekleyin:

https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/repo.json

Kısa kod ([tiny](https://tinyurl.com/) ile oluşturulması gerekir): manitrepo

## Eklenti geliştiricileri için repo hazırlığı

Yeni eklenti geliştirmeye başlamak için örnek proje olarak [PluginTemplate](PluginTemplate/README.md) kullanılabilir. Template README'si derleme, yayınlama ve hazır extractor sınıflarıyla video kaynaklarını çözümleme akışını özetler.

Bu repo tek dal üzerinden yayınlanır:

- `main` dalı kaynak kodunu, üst repo manifestini ve yayın çıktılarını birlikte tutar.
- `builds/` dizini derlenmiş eklenti DLL'lerini ve Manitux'un okuyacağı eklenti listesini tutar.

`repo.json`, Manitux uygulamasına eklenen ana manifesttir. Bu dosyadaki `pluginLists` alanı şu anda `main` dalı altındaki `builds/plugins.json` dosyasını işaret eder:

```json
"pluginLists": [
  "https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/builds/plugins.json"
]
```

Bu nedenle yeni bir eklentiyi yayınlamak için yalnızca kaynak kodu eklemek yeterli değildir; derlenmiş DLL ve metadata da repo içindeki `builds/` dizinine eklenmelidir.

Yerel çalışma düzeni şöyledir:

- Kaynak repo: `manitux-plugins`
- Yayın/build dizini: `builds/`
- Yayın listesi: `builds/plugins.json`
- Yayın DLL'leri: `builds/*.dll`

Yeni veya güncellenen bir eklenti hazırlarken genel akış:

1. Eklenti sınıfını `Manitux.Plugins/` altına ekle veya güncelle.
2. Eklentinin `PluginManifest.Id` değerini benzersiz tut. Bu değer `plugins.json` içindeki `internalName` ile aynı olmalıdır.
3. Projeyi derle:

```bash
dotnet build Manitux.Plugins/Manitux.Plugins.csproj -c Release
```

4. Oluşan DLL'i `builds/` altına kopyala. Ortak paket için beklenen dosya adı şu anda `Manitux.Plugins.dll` olarak kullanılıyor.
5. `builds/plugins.json` dosyasında ilgili DLL kaydını güncelle.

`plugins.json` içindeki her DLL kaydı şu bilgileri taşır:

- `url`: DLL'in `main` dalı altındaki `builds/` dizininde bulunan raw GitHub adresi.
- `status`: Eklentinin yayın durumu. Aktif kayıtlar için `1` kullanılır.
- `version`: DLL paket versiyonu. DLL içeriği değiştiğinde artırılmalıdır.
- `apiVersion`: Manitux plugin API versiyonu.
- `authors`: Eklenti geliştiricileri.
- `repositoryUrl`: Kaynak repo adresi.
- `plugins`: DLL içindeki eklentilerin Manitux'ta görünecek metadata listesi.

`plugins` listesindeki her eklenti için en az şu alanlar doldurulmalıdır:

- `name`: Kullanıcıya görünen ad.
- `internalName`: Kod tarafındaki manifest `Id` değeri.
- `description`: Kısa açıklama.
- `language`: Varsayılan dil kodu.
- `iconUrl`: İkon adresi.
- `isAdult`: Yetişkin içerik bilgisi.
- `tvTypes`: Desteklenen içerik tipleri.

Kaynak kodu, `builds/` altındaki DLL dosyaları ve `builds/plugins.json` değişiklikleri aynı `main` dalına commitlenmelidir. Manitux uygulaması `repo.json` üzerinden `builds/plugins.json` dosyasına, oradan da raw DLL URL'lerine ulaşır.
