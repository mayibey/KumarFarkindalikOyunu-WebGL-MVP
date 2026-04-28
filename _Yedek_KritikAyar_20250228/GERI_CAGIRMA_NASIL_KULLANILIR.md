# Geri Çağırma — Kritik Ayar Yedeği (28 Şubat 2025)

Bu klasör, **Kritik Ayar ve Spin Akışı** değişikliklerine başlamadan hemen önce alınan **tam yedek**tir.  
`Assets/Scripts` ve `Assets/Scripts/Services` içeriği burada `Scripts/` altında saklanır.

---

## Geri çağırma kodu (bana yazacağın cümle)

Projeyi bu yedekteki haline döndürmek istediğinde Cursor’da bana şunu yaz:

```
BACKUP_KRITIK_AYAR_20250228 geri yükle
```

Ben bu kodu gördüğümde `_Yedek_KritikAyar_20250228\Scripts` içeriğini `Assets\Scripts` üzerine kopyalayarak geri yükleme adımlarını uygulayacağım.

---

## Elle geri yükleme (sen yapacaksan)

1. **Mevcut Scripts’i yedekle** (isteğe bağlı):  
   `Assets\Scripts` klasörünü başka bir isimle kopyala (örn. `Scripts_Simdi`).

2. **Yedekten geri kopyala:**  
   `_Yedek_KritikAyar_20250228\Scripts` içindeki **tüm dosya ve klasörleri**  
   `Assets\Scripts` konumuna kopyala (üzerine yaz).

   PowerShell örneği (proje kökünden):
   ```powershell
   Copy-Item -Path "_Yedek_KritikAyar_20250228\Scripts\*" -Destination "Assets\Scripts" -Recurse -Force
   ```

3. Unity’yi yeniden derle; gerekirse **Assets > Reimport** veya projeyi kapatıp aç.

---

## Bu yedeğin içeriği

- **Tarih:** 28 Şubat 2025 (Kritik Ayar uygulama planı öncesi)
- **Kapsam:** `Assets/Scripts` altındaki tüm `.cs` ve ilgili dosyalar (Services dahil)
- **Referans:** `KRITIK_AYARLAR_VE_SPIN_AKISI_ANALIZI.md` ve uygulama planı bu yedekten **sonra** uygulanacak değişiklikleri tanımlar.

Bu klasörü silme; geri dönmek istediğinde yukarıdaki kodu kullan.
