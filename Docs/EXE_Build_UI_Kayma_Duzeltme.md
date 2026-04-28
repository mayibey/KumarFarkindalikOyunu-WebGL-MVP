# EXE Build'ta UI Kayması Nasıl Düzeltilir?

Build aldığınızda "Bakiye / Bahis" yazıları ızgara ile üst üste biniyorsa veya öğeler kayıyorsa aşağıdakileri uygulayın.

## 1. Build UILayout Fix script'i (Kod)

- **02_SenaryoluOyun** sahnesini açın.
- Hiyerarşide **ana Canvas**'ı seçin (oyun UI'ının olduğu Canvas).
- Inspector'da **Add Component** → `BuildUILayoutFix` ekleyin.
- Bu script sahne açıldığında layout'u zorla günceller; build'de kayma çoğu zaman azalır.

## 2. Unity Editor'da Canvas Scaler

- Aynı **Canvas**'ı seçin.
- **Canvas Scaler** bileşenini kontrol edin:
  - **UI Scale Mode:** `Scale With Screen Size`
  - **Reference Resolution:** `1920 x 1080` (veya hedeflediğiniz çözünürlük)
  - **Match:** `0.5` (en-boy dengesi; sadece genişlik için 0, sadece yükseklik için 1)
- **Reference Pixels Per Unit:** 100 (değiştirmeyin gerekmedikçe).

## 3. Bakiye / Bahis metinlerinin kesilmemesi

Izgara (slot grid) ile "Bakiye" / "Bahis" yazıları üst üste biniyorsa:

- **SlotGrid** (veya sembol ızgarasının parent'ı) ile **Bakiye/Bahis** metinlerinin bulunduğu paneli aynı **parent** altında tutun.
- Bu parent'a **Vertical Layout Group** ekleyin:
  - **Spacing:** 8–16
  - **Child Alignment:** Upper Center
  - **Child Force Expand:** Height = false (veya ihtiyaca göre)
- Böylece ızgara ile altındaki metinler arasında sabit boşluk olur ve build'de kayma azalır.

Alternatif (layout kullanmıyorsanız):

- **SlotGrid**'in **RectTransform**'unda **Bottom** değerini artırın (örn. -20 → -40) ki altında daha fazla boşluk kalsın.
- Veya Bakiye/Bahis metinlerinin olduğu panelin **Top** değerini aşağı çekin (daha fazla negatif) ki ızgaranın altına taşınsın.

## 4. Build ayarlarında çözünürlük

- **File → Build Settings** → **Player Settings** → **Resolution and Presentation**
- **Default Screen Width / Height:** 1920x1080 (veya kullandığınız reference resolution ile aynı)
- **Fullscreen Mode:** Windowed veya Fullscreen ihtiyacınıza göre

Editor'da test için:

- **Game** penceresinde **Free Aspect** yerine **1920x1080** (veya build çözünürlüğünüz) seçin; UI'ı bu oranda kontrol edin.

## 5. Üst başlık (KAZANÇ vb.) kayması

Üstteki başlık metni diğer öğelerle çakışıyorsa:

- Başlık **RectTransform**'unda **Anchor** üst orta (top-center) ve **Pivot** (0.5, 1) olsun.
- **Top** offset ile aşağı itin (örn. -10 veya -20) ki diğer UI ile çakışmasın.

---

Özet: `BuildUILayoutFix`'i ana Canvas'a ekleyin, Canvas Scaler'ı Scale With Screen Size + 1920x1080 + Match 0.5 yapın, Bakiye/Bahis ile ızgara arasında Vertical Layout Group veya manuel boşluk verin.
