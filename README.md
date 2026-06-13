[English](README_en.md)  

# Manitux Eklenti Deposu

Bu depo, [Manitux](https://github.com/manitux-app/manitux) için bir eklenti koleksiyonu içermektedir.

Repo.json dosyasının ham URL'sini Manitux uygulama ayarları sayfasına ekleyin:

https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/repo.json

Kısa kod (cutt.ly ile oluşturulması gerekir): manitrepo

## Eklenti gelistiricileri icin repo hazirligi

Bu repo iki parcali bir yayin akisi kullanir:

- `main` dali kaynak kodu ve ust repo manifestini tutar.
- `builds` dali derlenmis eklenti DLL'lerini ve Manitux'un okuyacagi eklenti listesini tutar.

`repo.json`, Manitux uygulamasina eklenen ana manifesttir. Bu dosyadaki `pluginLists` alani su anda `builds` dalindaki `plugins.json` dosyasini isaret eder:

```json
"pluginLists": [
  "https://raw.githubusercontent.com/manitux-app/manitux-plugins/builds/plugins.json"
]
```

Bu nedenle yeni bir eklentiyi yayinlamak icin yalnizca kaynak kodu eklemek yeterli degildir; derlenmis DLL ve metadata da `builds` tarafina eklenmelidir.

Yerel calisma duzeni soyledir:

- Kaynak repo: `manitux-plugins`
- Yayin/build repo: `../manitux-plugins-builds`
- Yayin listesi: `../manitux-plugins-builds/plugins.json`
- Yayin DLL'leri: `../manitux-plugins-builds/*.dll`

Yeni veya guncellenen bir eklenti hazirlarken genel akis:

1. Eklenti sinifini `Manitux.Plugins/` altina ekle veya guncelle.
2. Eklentinin `PluginManifest.Id` degerini benzersiz tut. Bu deger `plugins.json` icindeki `internalName` ile ayni olmalidir.
3. Projeyi derle:

```bash
dotnet build Manitux.Plugins/Manitux.Plugins.csproj -c Release
```

4. Olusan DLL'i `../manitux-plugins-builds/` altina kopyala. Ortak paket icin beklenen dosya adi su anda `Manitux.Plugins.dll` olarak kullaniliyor.
5. `../manitux-plugins-builds/plugins.json` dosyasinda ilgili DLL kaydini guncelle.

`plugins.json` icindeki her DLL kaydi su bilgileri tasir:

- `url`: DLL'in `builds` dalindaki raw GitHub adresi.
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

Kaynak kodu `main` dalina, `../manitux-plugins-builds` klasorundeki DLL ve `plugins.json` degisiklikleri ise `builds` dalina commitlenmelidir. Manitux uygulamasi `repo.json` uzerinden `plugins.json` dosyasina, oradan da raw DLL URL'lerine ulasir.
