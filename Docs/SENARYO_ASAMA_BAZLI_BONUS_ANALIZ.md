# Senaryo Aşama Bazlı “Özel Bonus Oyun” Analizi

**Soru:** Her senaryo için önceden belli, ayarlı bonus oyun tanımı olsa; bu tanımlar önceki limitlere (havuz, genel zorluk vb.) hiç takılmadan kendi iç yapısında çalışsa mantıklı mı?

---

## 1. Mevcut durum (kısa)

- Bonus bütçe/tavanı **ödül havuzundan** (Kasa) türetiliyor: `InitBonusBudgetFromHavuz(GetHavuzTL())`.
- Zorluk, **genel aşama ayarından** geliyor (`AsamaAyariniUygula` → `SetZorluk`); bonus da aynı zorluğu kullanıyor.
- Senaryo 1/2 için **özel patch’ler** var (min cap 8k, satın alma maliyet+%30); diğer aşamalar hâlâ havuz/limitlere bağlı.
- Sonuç: Havuz küçükse veya limitler sıkıysa bonus “az ödüyor”; spec’teki aşama bazlı bonus davranışı tam uygulanmıyor.

---

## 2. Önerilen model: “Aşama bazlı bonus config”

Her senaryo aşaması (1–7) için **sadece o aşamaya özel** bir bonus tanımı:

- **Max toplam ödeme** (TL)
- **Bonus içi zorluk** (isteğe bağlı; normal spinden bağımsız)
- **Scatter vs satın alma** için ayrı formüller (spec’teki gibi: +%30, −%20 vb.)
- İsteğe bağlı: spin sayısı, çarpan/tumble politikası (örn. Aşama 3: yüksek çarpan göster, tumble yok)

Bu config **referans olarak**:
- Havuzu (Kasa) **kullanmaz**
- Genel “ödenebilir tutar” / diğer global limitleri **kullanmaz**
- Sadece **mevcut aşama** + **bonus tetikleme tipi** (scatter / satın al) ile okunur ve uygulanır.

Yani bonus, “kendi içinde tanımlı” çalışır; önceki limitlere takılmaz.

---

## 3. Mantıklı mı? – Evet

| Gerekçe | Açıklama |
|--------|----------|
| **Spec ile uyum** | `SENARYO_ASAMA_ZORLUK_VE_AYARLAR_SPEC.md` zaten aşama bazlı bonus kurallarını veriyor (Aşama 1: +%30, Aşama 2: +%20, Aşama 3: −%20, Aşama 4: −%50, Aşama 5: 2,5x, Aşama 6: etkisiz, Aşama 7: kazanç kapalı). Bu kurallar doğrudan “aşama bonus config”e yazılabilir. |
| **Öngörülebilir tasarım** | Her aşamanın hikayesi (ısındırma / kontrol / az daha / tükeniş / zirve / kayıp / finale) kendi bonus davranışına yansır; havuz veya kasa değişince senaryo bonusu değişmez. |
| **Bağımsız çalışma** | “Önceki limitlere, zorluğa vs hiç takılmadan kendi iç yapısında çalışsın” isteği tam olarak bu: bonus sadece **aşama + tetikleme tipi** ile config’ten okunan değerlerle çalışır. |
| **Bakım / denge** | Tüm senaryo bonusları tek yerden (ScriptableObject, tablo veya data class) yönetilir; 7 aşama × (max ödeme, zorluk, formül) netleşir. |
| **Test** | Her aşama bonusu ayrı test edilebilir; “Aşama 1 bonus her zaman X TL bandında” gibi hedefler konur. |

Dikkat edilmesi gereken tek tasarım kararı: **Oturum “ödenebilir tutar” ile çakışma.**  
İki seçenek:

- **A) Tam bağımsız:** Senaryo bonusu sadece kendi cap’i ile çalışır; gerekirse oturum bütçesini aşabilir (ısındırma için makul).
- **B) Üst sınır olarak ödenebilir:** Bonus cap = min(aşama bonus cap’i, kalan ödenebilir tutar).  
Spec ve “kendi iç yapısı” vurgusu **A**’ya daha yakın; istersen B de “son kırpma” olarak eklenebilir.

---

## 4. Önerilen yapı (kaba)

- **Veri:** `SenaryoAşamaBonusConfig` (aşama 1–7 için):
  - `maxToplamOdemeScatterTL` veya formül (bahis %X, sabit, vb.)
  - `maxToplamOdemeSatinAlmaFormül` (maliyet + %30, maliyet − %20, 2.5× maliyet vb.)
  - `bonusIciZorluk` (opsiyonel; yoksa genel aşama zorluğu kullanılır)
  - İsteğe bağlı: `tumbleKapali` (Aşama 3), `etkisizBonus` (Aşama 6), `kazancTamamenKapali` (Aşama 7)

- **Akış:** Bonus başlarken (`BaslatBonus` veya eşdeğeri):
  - Eğer `SenaryoYoneticisi.I != null` ve senaryo sahnesindeysek:
    - `mevcutAsama` + tetikleme tipi (scatter / satın al) ile ilgili **aşama bonus config** seçilir.
    - `_bonusMaxOdemeTL` ve gerekirse `_bonusBudgetKalanTL` **sadece bu config’ten** set edilir; havuz/global limit **okunmaz**.
  - Admin / normal oyunda senaryo yoksa: mevcut mantık (havuz + genel limitler) aynen kalır.

Böylece “her senaryo için özel, ayarlı bonus; önceki limitlere takılmadan kendi iç yapısında çalışır” hedefi karşılanır.

---

## 5. Kısa cevap

**Evet, mantıklı.**  
Her senaryo için önceden belli, aşama bazlı bonus tanımları kullanmak ve bunların havuz/önceki limitlere hiç bakmadan kendi içinde çalışması:
- Spec’e uyumlu,
- Tasarladığın “ısındırma / kontrol / az daha / …” hikayesiyle uyumlu,
- Bakım ve test açısından sade.

İstersen bir sonraki adımda `SenaryoAşamaBonusConfig` alanları ve `BaslatBonus` içinde nereye bağlanacağı madde madde çıkarılabilir.
