# Log Sahnesi – İki Bölümlü Profesyonel Rapor Kurulumu

Log sahnesi artık **iki bölümden** oluşuyor:

1. **Bölüm 1 – Genel özet:** Toplam spin, yatırılan/çekilen, net, bakiye, bonus sayıları, senaryoda ulaşılan aşama, bahis değişikliği sayısı.
2. **Bölüm 2 – Senaryoya özgü log:** Senaryo olayları **aşama aşama** (1–7) gruplanmış; her aşamada zaman, olay tipi, açıklama, bakiye ve net gösterilir. Amaç: sistemin kullanıcıyı nasıl yönlendirdiğini göstermek.

---

## Seçenek A: Tek scroll (en basit)

- Sahnedeki mevcut **Scroll_LogRapor** → **Viewport** → **Content** aynen kalsın.
- **LogYoneticisi** component’ini (örn. LogArayuzu veya Panel_LogRapor üzerinde) kullanmaya devam edin.
- **Genel Ozet Text** ve **Senaryo Log Content** alanlarını atamayın; script tüm içeriği tek scroll’a yazar.
- Sonuç: Aynı scroll içinde önce genel özet bloku, hemen altında aşama başlıkları ve senaryo log satırları listelenir.

---

## Seçenek B: İki ayrı alan (daha okunabilir)

### 1) Genel özet alanı

- **Panel_LogRapor** (veya üst kısımda ayrı bir panel) içinde bir **TextMeshPro - Text (UI)** ekleyin.
- İsim: **GenelOzetText**. Inspector’da atanmazsa script bu isimle sahnede TMP_Text arar.
- Bu metin kutusu genel özeti tek blok halinde gösterir; gerekiyorsa Scroll View dışında sabit yükseklikte tutun.

### 2) Senaryo log alanı

- Ayrı bir **Scroll View** oluşturun (örn. **Scroll_SenaryoLog**).
- İçinde **Viewport** → **Content** olacak şekilde hiyerarşiyi kurun.
- **Content** objesinin adını **Content_SenaryoLog** yapın (script isimle bulur).
- LogYoneticisi’nde **Senaryo Log Content** alanına bu Content’i sürükleyin (veya isim eşleşince otomatik bulunur).

### 3) Ana scroll (Content)

- Eski tek scroll’u sadece senaryo logu için kullanacaksanız: **logContent** alanına bu scroll’un **Content**’ini atayın ve **GenelOzetText**’i doldurun. Böylece üstte özet, altta senaryo logu iki ayrı bölüm olur.

---

## Inspector’da LogYoneticisi alanları

| Alan | Açıklama |
|------|----------|
| **Kullanici Bilgi Text** | Başlık (örn. "ada123 - İstatistikleri"). |
| **Genel Ozet Text** | Bölüm 1 metni. Atanmazsa özet tek scroll’un en üstüne blok olarak yazılır. |
| **Senaryo Log Content** | Bölüm 2’nin ekleneceği Content. Atanmazsa **logContent** kullanılır. |
| **Log Content** | Ana scroll’un Content’i (Scroll_LogRapor/Viewport/Content). |
| **Log Satir Prefab** | İsteğe bağlı; atanmazsa satırlar kodla oluşturulur. |
| **Geri Don Buton** | Girişe dön butonu (Buton_GirisEkrani). |

---

## Özet metni içeriği

- Kullanıcı adı, toplam spin, toplam yatırılan/çekilen, net, bakiye.
- Toplam kazanç, toplam kayıp, bonus giriş ve bonus satın alma sayıları.
- Senaryo açıksa: ulaşılan aşama, oturumda yatırılan, bahis değişikliği sayısı.
- Son satır: “Bu rapor, senaryolu oyunda sistemin davranışını ve oturum akışını göstermek için hazırlanmıştır.”

## Senaryo logu içeriği

- Olaylar **aşama numarasına göre** gruplanır (1–7).
- Her grup: **▼ AŞAMA X: [Aşama adı]** başlığı + o aşamadaki tüm olaylar (zaman, olay tipi, açıklama, bakiye, net).
- Olay tipleri: AsamaGirisi, AsamaCikisi, BakiyeYukleme, ManuelGecis, SartTamamlandi.
