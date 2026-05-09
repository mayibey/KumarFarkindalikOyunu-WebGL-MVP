# Test Mekanizması – Sentetik Oyuncu ve Aşama Geçiş Optimizasyonu (İleride Uygulanacak)

Bu dosya, ileride uygulanmak üzere fikir ve notları içerir. Sohbette "bu konuyla ilgili notlarını al ilerde sana soracağım" denildi.

---

## Amaç

1. **Gerçek kullanıcı gibi test:** Uygulamayı otomatik oynatan bir “sentetik oyuncu” (bot/simülasyon) ile senaryolu oyunu test etmek.
2. **Her aşamanın geçiş şartlarının optimalini bulma:** Hangi koşul setlerinin (spin sayısı, bakiye, yükleme, bahis artırımı vb.) aşama geçişlerini en iyi tetiklediğini deneyerek bulmak.
3. **Farklı karakteristiklerle oynama:** Agresif (sık bahis artırır), temkinli (az spin), “bonus kovalayan” gibi farklı oyuncu profilleriyle senaryonun davranışını ölçmek.

---

## Teknik Özet

- **Oyunu kodla oynatma:** Spin, bahis artır/azalt, bonus satın alma, bakiye yükleme gibi aksiyonları API/callback veya doğrudan servis çağrılarıyla yapan bir test/simülasyon katmanı (Unity içi test modu veya editörde simülasyon sahnesi).
- **Karakteristik = strateji:** Her sentetik oyuncunun bir stratejisi olur (kurallı veya olasılıksal): örn. “Her 50 spinde bahis artır”, “bakiye X altına düşünce yükle”, “bonus satın alan” vb.
- **Aşama geçişlerini izleme:** SenaryoYoneticisi zaten aşama ve geçişleri takip ediyor; test katmanı hangi spinde, hangi koşullarda aşama değiştiğini loglayıp analiz edebilir.
- **Optimal şartları bulma:** Parametre taraması (grid), Monte Carlo (çok sayıda rastgele oyun), veya optimizasyon (genetik algoritma, Bayesian optimization vb.) ile “en az spinle aşama 3’e ulaş” gibi hedefler için optimal (veya iyi) koşul setleri bulunabilir.

---

## Projede Konum

- Senaryo motoru (aşamalar, şartlar, loglar) mevcut.
- Eksik: Oyunu otomatik oynatan test/simülasyon katmanı, farklı profillerle çalıştırma, sonuçları loglama/raporlama (hangi koşulda kaç spinde hangi aşamaya geçildi vb.).

---

## Sonuç

- Evet, mümkün: hem “gerçek kullanıcı gibi test” hem “aşama geçiş şartlarının optimalini bulma” hem “farklı karakteristiklerle oynama” tek bir test/simülasyon çatısıyla yapılabilir.
- İleride bu konu açıldığında bu notlar ve mevcut SenaryoYoneticisi / OyunYoneticisi yapısı referans alınacak.
