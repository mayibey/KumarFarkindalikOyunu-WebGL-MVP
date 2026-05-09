# Panel Kurulum Talimatları

## Unity WebGL Projene Kurulum — 6 Adım

### Adım 1: Dosyaları Projeye Kopyala

Unity projenin `Assets` klasöründe aşağıdaki yapıyı oluştur:

```
Assets/
├── StreamingAssets/
│   └── panel.html          ← Bu dosyayı buraya kopyala
├── Plugins/
│   └── WebGL/
│       └── PanelBridge.jslib  ← Bu dosyayı buraya kopyala
└── Scripts/
    └── PanelKopru.cs       ← Bu dosyayı buraya kopyala
```

**Önemli:**
- `StreamingAssets` klasörü yoksa oluştur (büyük harfe dikkat)
- `Plugins/WebGL` klasörü yoksa oluştur
- `.jslib` uzantısına dokunma, aynen koru

### Adım 2: Sahneye PanelKopru Objesi Ekle

1. Hierarchy'de boş bir GameObject oluştur: `GameObject → Create Empty`
2. Adını **tam olarak** `PanelKopru` yap (büyük-küçük harf önemli)
3. `PanelKopru.cs` script'ini bu objeye sürükle-bırak

### Adım 3: Ayarlar Butonuna Bağla

Senin oyununda ayarlar butonun nerede bilmiyorum ama akış şöyle:

**Eğer Canvas içinde bir UI Button'sa:**
1. Butonu seç
2. Inspector'da `OnClick()` kısmında `+` butonuna bas
3. Sahnedeki `PanelKopru` objesini sürükle
4. Fonksiyon olarak `PanelKopru.AyarlarButonunaBasildi` seç

**Eğer kendi script'inden çağırıyorsan:**
```csharp
// Ayarlar butonuna basılınca çalışan fonksiyonunda:
FindObjectOfType<PanelKopru>().AyarlarButonunaBasildi();
```

### Adım 4: Oyun Ayarlarını Bağla

`PanelKopru.cs` içinde `AyariIsle` fonksiyonu var. Her `case` bloğunun altında
**yorum satırı** halinde örnek bağlantılar yazdım:

```csharp
case "kazanmaOrani":
    kazanmaOrani = float.Parse(deger);
    // GameManager.Instance.SetKazanmaOrani(kazanmaOrani);  ← Bunu aç
    break;
```

**Senin yapman gereken:** Bu yorum satırlarını kendi oyun kodlarına göre aç.
Örnek:
- Eğer GameManager'ın `SlotAyarlari` diye bir script'se:
  ```csharp
  SlotAyarlari.kazanmaOrani = kazanmaOrani;
  ```
- Eğer PlayerPrefs kullanıyorsan:
  ```csharp
  PlayerPrefs.SetFloat("kazanmaOrani", kazanmaOrani);
  ```

### Adım 5: WebGL Build Ayarları

1. `File → Build Settings`
2. Platform olarak **WebGL** seçili olmalı
3. `Player Settings → Resolution and Presentation`
4. WebGL Template: **Default** (özel template değilse)
5. `Player Settings → Publishing Settings`
6. Compression Format: **Disabled** (test ederken kolaylık için, sonra Brotli yapabilirsin)

### Adım 6: Build ve Test

1. `Build Settings → Build` bas, bir klasör seç
2. Build bitince o klasörde `index.html` oluşacak
3. **Önemli:** Unity WebGL build'i direkt açılmaz, bir sunucuda çalıştırman lazım:

**En kolay yol (Python yüklüyse):**
```bash
cd build-klasorun
python -m http.server 8000
```
Sonra tarayıcıda: `http://localhost:8000`

**Alternatif:** VS Code'un "Live Server" eklentisi.

---

## Test Akışı

1. Build'i tarayıcıda aç
2. Oyun yüklensin
3. Ayarlar butonuna bas
4. Panel açılmalı
5. Slider'ı oynat
6. F12 ile konsolu aç, Unity log'larında `[PanelKopru] kazanmaOrani = 75` gibi mesajlar görmelisin
7. Bu mesajlar görünüyorsa her şey çalışıyor demektir

---

## Sık Karşılaşılan Sorunlar

**"Panel açılmıyor"**
- `StreamingAssets/panel.html` doğru yerde mi?
- Sahnedeki GameObject adı tam olarak `PanelKopru` mi?
- Build'i sunucuda mı çalıştırıyorsun? (file:// ile olmaz)

**"Panel açılıyor ama Unity ayarları almıyor"**
- Konsol'da hata var mı? F12 aç, Console sekmesine bak.
- `.jslib` dosyası `Assets/Plugins/WebGL/` içinde mi?
- PanelKopru objesinin adı **tam olarak** `PanelKopru` mi? Büyük-küçük harf önemli.

**"Editor'de çalıştırıyorum, panel gözükmüyor"**
- Panel sadece WebGL build'de çalışır.
- Editor'de test için: panel.html'i direkt tarayıcıda aç, F12 ile konsolu gör, mesajlar `[Panel → Unity]` olarak logged olur.

---

## İleri Düzey — Video İçin Split-Screen Layout

Videoda sol tarafta oyun, sağ tarafta panel göstermek istersen:

`Assets/WebGLTemplates/Custom/index.html` diye özel bir template oluşturup
Unity canvas'ını sola, panel iframe'ini sağa yerleştirebilirsin. Bu template'i
sonra istersen yazabilirim, şimdilik standart kurulum yeterli.
