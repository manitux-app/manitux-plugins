[English](README_en.md)  

# Manitux Eklenti Deposu

Bu depo, [Manitux](https://github.com/manitux-app/manitux) için bir eklenti koleksiyonu içermektedir.

Repo.json dosyasının ham URL'sini Manitux uygulama ayarları sayfasına ekleyin:

https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/repo.json

Kısa kod (cutt.ly ile oluşturulması gerekir): manitrepo

## Eklenti gelistiricileri icin repo hazirligi

Bu repo tek dal uzerinden yayinlanir:

- `main` dali kaynak kodunu, ust repo manifestini ve yayin ciktilarini birlikte tutar.
- `builds/` dizini derlenmis eklenti DLL'lerini ve Manitux'un okuyacagi eklenti listesini tutar.

`repo.json`, Manitux uygulamasina eklenen ana manifesttir. Bu dosyadaki `pluginLists` alani su anda `main` dali altindaki `builds/plugins.json` dosyasini isaret eder:

```json
"pluginLists": [
  "https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/builds/plugins.json"
]
```

Bu nedenle yeni bir eklentiyi yayinlamak icin yalnizca kaynak kodu eklemek yeterli degildir; derlenmis DLL ve metadata da repo icindeki `builds/` dizinine eklenmelidir.

Yerel calisma duzeni soyledir:

- Kaynak repo: `manitux-plugins`
- Yayin/build dizini: `builds/`
- Yayin listesi: `builds/plugins.json`
- Yayin DLL'leri: `builds/*.dll`

Yeni veya guncellenen bir eklenti hazirlarken genel akis:

1. Eklenti sinifini `Manitux.Plugins/` altina ekle veya guncelle.
2. Eklentinin `PluginManifest.Id` degerini benzersiz tut. Bu deger `plugins.json` icindeki `internalName` ile ayni olmalidir.
3. Projeyi derle:

```bash
dotnet build Manitux.Plugins/Manitux.Plugins.csproj -c Release
```

4. Olusan DLL'i `builds/` altina kopyala. Ortak paket icin beklenen dosya adi su anda `Manitux.Plugins.dll` olarak kullaniliyor.
5. `builds/plugins.json` dosyasinda ilgili DLL kaydini guncelle.

`plugins.json` icindeki her DLL kaydi su bilgileri tasir:

- `url`: DLL'in `main` dali altindaki `builds/` dizininde bulunan raw GitHub adresi.
- `status`: Eklentinin yayin durumu. Aktif kayitlar icin `1` kullanilir.
- `version`: DLL paket versiyonu. DLL icerigi degistiginde artirilmalidir.
- `apiVersion`: Manitux plugin API versiyonu.
- `authors`: Eklenti gelistiricileri.
- `repositoryUrl`: Kaynak repo adresi.
- `plugins`: DLL icindeki eklentilerin Manitux'ta gorunecek metadata listesi.

`plugins` listesindeki her eklenti icin en az su alanlari doldurulmalidir:

- `name`: Kullaniciya gorunen ad.
- `internalName`: Kod tarafindaki manifest `Id` degeri.
- `description`: Kisa aciklama.
- `language`: Varsayilan dil kodu.
- `iconUrl`: Ikon adresi.
- `isAdult`: Yetiskin icerik bilgisi.
- `tvTypes`: Desteklenen icerik tipleri.

Kaynak kodu, `builds/` altindaki DLL dosyalari ve `builds/plugins.json` degisiklikleri ayni `main` dalina commitlenmelidir. Manitux uygulamasi `repo.json` uzerinden `builds/plugins.json` dosyasina, oradan da raw DLL URL'lerine ulasir.
