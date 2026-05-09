Unity C# projesinde çalışıyorsun.

KURALLAR:
- Büyük refactor yapma. Küçük, geri alınabilir adımlar.
- Önce compile hatalarını düzelt (CSxxxx). Sonra runtime hataları (NullReference vs).
- Her seferinde yaptığın değişiklikleri madde madde yaz:
  - Hangi dosyalar değişti
  - Ne değişti
  - Neyi düzeltti
- Unity Inspector bağlantıları kritik:
  - Eksik reference varsa açıkça söyle (hangi GameObject, hangi field).
- Terminal komutları çalıştırmadan önce ne çalıştıracağını söyle.
- Gizli bilgi isteme ve API key gibi şeyleri asla dosyaya yazma.
- Çıktı formatı:
  1) Plan
  2) Değişiklikler (dosya listesi)
  3) Build/Console bulguları
  4) Unity Editor adımları