using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DonusAkisServisi için state ve servis erişimi arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IDonusAkisBaglami
{
    UIServisi UIServisi { get; }
    IzgaraServisi IzgaraServisi { get; }
    OdemeServisi OdemeServisi { get; }
    AnimasyonServisi AnimasyonServisi { get; }
    TumbleServisi TumbleServisi { get; }
    CarpanServisi CarpanServisi { get; }
    EkonomiServisi EkonomiServisi { get; }
    LogServisi DonusKayitServisi { get; }
    SenaryoServisi SenaryoServisi { get; }
    HizVeSesServisi HizVeSesServisi { get; }
    bool SpinCalisiyor { get; set; }
    bool BonusAktif { get; set; }
    int BonusHakKalan { get; set; }
    int BonusKazanc { get; set; }
    int OturumKazanc { get; set; }
    int BonusPendingOdemeTL { get; set; }
    int BonusZorlaCarpanBirikenTL { get; set; }
    bool SpinKazanciOturumaEklendi { get; set; }
    int SpinKazancHam { get; set; }
    int TumbleToplamKazanc { get; set; }
    int SonSpinKazanci { get; set; }
    int SpinPrevBakiye { get; }
    int SpinBahisTL { get; }
    float BonusSpinBekleme { get; }
    int SonSpinKazancHamGoster { set; }
    int SonSpinCarpanGoster { set; }
    int SonSpinKazancToplamGoster { set; }
    int BonusOturumOdenenToplamTL { get; set; }
    int BonusMaxOdemeTL { get; }
    int BonusOdenenTL { get; }
    bool BonusBudgetAktif { get; }
    int BonusBudgetKalanTL { get; }
    int[,] Grid { get; }
    int Satir { get; }
    int Sutun { get; }
    int spinNo { get; set;}
    void UI_CarpanSifirla();
    /// <summary>Çarpan kutuya uçuşu bitti; spin özeti alanları yazıldıktan sonra çağrılır. Uçuş sonrası–ödeme öncesi ara karede kazanç metninin ham TL'ye düşmesini engeller.</summary>
    void CarpanKutuUcusFormulKilidiniKaldir();
    /// <summary>Spin sonunda çarpan formülünü (X TL × N = Y TL) sonraki spin başlayıncaya kadar ekranda tut.</summary>
    void CarpanFormulGosterAktivateEt(int birikimSonDeger);
    void CarpanUretVeBirik();
    void CarpanlariDoluGriddeUygula();
    void BaslatBonus();
    IEnumerator ScatterBuyutEfekti();
    IEnumerator ShowBonusEndMessage(int bonusToplamKazanc);
    void SetSpinIconRotate(bool rotate);
    void SetOturumKazancTextActive(bool active);
    void NormalOyunMusicPlay();
    void NormalOyunMusicUnPause();
    /// <summary>Arkada simülasyon çalıştırır; limiti aşmayan sonucu kaydeder. Re-roll max 200.</summary>
    SpinSimulasyonKaydi SimuleEtVeKaydet(int odenebilirLimit, bool bonusSpin);
    /// <summary>Önceden hesaplanan spin varsa ve türü uyuşuyorsa tüketir; anında tepki için kullanılır.</summary>
    bool TryConsumeOncedenHesaplanan(bool forBonusSpin, out SpinSimulasyonKaydi kayit);
    /// <summary>Bir sonraki spin'i spin bittikten sonra arka planda hesaplatır; butona basınca gecikme olmaz.</summary>
    void StartPrecomputeNextSpin(int odenebilirLimit, bool bonusSpin);
    /// <summary>Admin senaryo 2/3: K-KY-K... dizisi yalnızca oyuncuya gösterilen spin bittikten sonra ilerler; önbellek simülasyonunda ilerletilmez.</summary>
    void Senaryo2Veya3SpinSonundaUstUsteDonguIlerlet();
    /// <summary>Senaryo 2/3 aktifken bu spin ödeme bandına uygun üretildiyse döngü adımı ilerletilir.</summary>
    bool Senaryo23SpinSonrasiDonguIlerletilmeliMi(SpinSimulasyonKaydi kayit);
    /// <summary>Kaydedilen spin sonucunu ekranda oynatır (ilk grid + drop-in + tumble adımları).</summary>
    IEnumerator SimulasyonKaydiniOynat(SpinSimulasyonKaydi kayit);
    /// <summary>Bonus bittikten sonra kalan otomatik spin varsa döngüyü yeniden başlatır.</summary>
    void TryResumeOtomatikSpin();
    /// <summary>Senaryo sahnesinde: ödeme yapıldıysa ödenebilir bütçeden düş, yapılmadıysa bahisi ekle.</summary>
    void SenaryoOdenebilirGuncelle(int odenen, int bahis);
    /// <summary>Otomatik spin döngüsü aktifse true; normal spin akışında ilk kare gecikmesi atlanır.</summary>
    bool OtomatikSpinAktifMi { get; }
    IEnumerator ShowNormalSpinSonucPopup(int odenen, int bahis);
    /// <summary>false ise zorla çarpan sonrası tumble zinciri tek adımda durur (cascades yok).</summary>
    bool CarpanTumbleAktif { get; }
    /// <summary>Panel: min ödeme garantisi (TL). Kazanan spinlerde ödeme bu değerin altına düşmez.</summary>
    int MinOdemeTL { get; }
    /// <summary>Panel: min ödeme garantisi — bahis çarpanı olarak. 0 = devre dışı. Örn: 0.5 → 0.5×bahis.</summary>
    float MinOdemeCarpan { get; }
    /// <summary>Panel: maks ödeme tavanı — bahis çarpanı olarak. 0 = devre dışı. Örn: 20 → 20×bahis.</summary>
    float MaksOdemeCarpan { get; }
    /// <summary>Panel: N ardışık kayıptan sonra zorunlu kırıntı eşiği.</summary>
    int ArdisikKayipLimiti { get; }
    int ArdisikKayipSayac { get; set; }
    /// <summary>Eşik aşıldığında bir SONRAKİ spin için "garanti cluster" bayrağını set eder ve önbelleği temizler.
    /// Sahte para yok; oynanacak spin gerçek cluster ile gerçek ödeme üretir.</summary>
    void SonrakiSpinKacisFrenlemeAktifEt();
    /// <summary>Bonus otomatik tetikleme periyodu (admin panel ayarı). 0 = kapalı.</summary>
    int BonusOtomatikSpinPeriyodu { get; }
    int BonusOtomatikSpinSayaci { get; set; }
    bool BonusOtomatikTetikSonrakiSpin { get; set; }
    /// <summary>Yakın Kaçırma (10'da N) admin paneli ayarı; 0 = kapalı.</summary>
    int YakinKacirmaDegeri10da { get; }
    /// <summary>Görsel near-miss enjeksiyon: 7 aynı sembol komşu hücrelere yerleştirilir (cluster oluşmaz).</summary>
    void GrideNearMissEnjekteEt();
}

/// <summary>
/// Normal dönüş ve bonus döngüsü akışı. State ve servis erişimi IDonusAkisBaglami ile; alt coroutine'ler Func ile çalıştırılır.
/// </summary>
public class DonusAkisServisi
{
    private IDonusAkisBaglami _ctx;
    private Func<IEnumerator, Coroutine> _runCoroutine;

    private void SettledSpinDegerleriniNormalizeEt(int odenen)
    {
        if (_ctx == null) return;
        int gercek = Mathf.Max(0, odenen);
        _ctx.SonSpinKazanci = gercek;
        _ctx.SonSpinKazancToplamGoster = gercek;
    }

    public void SetBaglam(IDonusAkisBaglami ctx)
    {
        _ctx = ctx;
    }

    public void SetRunCoroutine(Func<IEnumerator, Coroutine> run)
    {
        _runCoroutine = run;
    }

    public IEnumerator NormalSpinAkisi()
    {
        if (_ctx == null) yield break;

        int spinNo = (SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : 0) + 1;
        int bakiyeSnap = _ctx.EkonomiServisi != null ? _ctx.EkonomiServisi.Bakiye : 0;
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_NormalSpinBasladi, $"Normal spin başladı. Spin no: {spinNo}. Bakiye: {bakiyeSnap:N0} TL. Bahis: {_ctx.SpinBahisTL} TL.");

        _ctx.SpinCalisiyor = true;
        _ctx.UIServisi?.ButonDurumu(false);
        _ctx.SetSpinIconRotate(true);

        _ctx.SpinKazancHam = 0;
        _ctx.TumbleToplamKazanc = 0;
        _ctx.SonSpinKazanci = 0;
        _ctx.SonSpinKazancHamGoster = 0;
        _ctx.SonSpinCarpanGoster = 1;
        _ctx.SonSpinKazancToplamGoster = 0;
        _ctx.UI_CarpanSifirla();
        _ctx.SpinKazanciOturumaEklendi = false;

        _ctx.IzgaraServisi?.ResetScatterCountPerSpin();
        _ctx.UIServisi?.UI_Guncelle();

        int odenebilirNormal = _ctx.OdemeServisi != null ? _ctx.OdemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        SpinSimulasyonKaydi kayit;

        if (!_ctx.TryConsumeOncedenHesaplanan(false, out kayit))
        {
            if (!_ctx.OtomatikSpinAktifMi)
                yield return null;

            _ctx.StartPrecomputeNextSpin(odenebilirNormal, false);

            const int maxBeklemeFrame = 5;
            int bekleme = 0;

            while (bekleme < maxBeklemeFrame && !_ctx.TryConsumeOncedenHesaplanan(false, out kayit))
            {
                bekleme++;
                yield return null;
            }

            if (kayit == null)
                kayit = _ctx.SimuleEtVeKaydet(odenebilirNormal, false);
        }

        if (_runCoroutine != null)
            yield return _runCoroutine(_ctx.SimulasyonKaydiniOynat(kayit));

        // Force carpan'da sisme animasyonu atla: sayı zaten doğru değerde; gereksiz pulsing kafa karıştırır.
        bool zorlaCarpanIcin = kayit != null && kayit.ZorlaCarpanKullanildi;
        if (_ctx.BonusAktif && !zorlaCarpanIcin && _runCoroutine != null && _ctx.AnimasyonServisi != null)
            yield return _runCoroutine(_ctx.AnimasyonServisi.AnimateCarpanSisme());

        // Ödeme hesabı: tumble toplamı kayıtta kayıtlı olsun (oynatma ile senkron); kayit yoksa context değerlerini kullan
        int hamKazanc = (kayit != null) ? kayit.ToplamHamKazanc : _ctx.SpinKazancHam;
        int toplamX = (kayit != null) ? kayit.NihaiCarpanToplam : _ctx.CarpanServisi.GetTotalMultiplierForSpin();
        int teorikToplam = _ctx.CarpanServisi.MulClampInt(hamKazanc, toplamX);
        bool zorlaCarpanKullanildi = kayit != null && kayit.ZorlaCarpanKullanildi;

        _ctx.SonSpinKazancHamGoster = hamKazanc;
        _ctx.SonSpinCarpanGoster = toplamX;
        _ctx.SonSpinKazancToplamGoster = teorikToplam;
        _ctx.SonSpinKazanci = 0;
        // Formül kilidi spin sonunda kalksın: yeni spin başlayana kadar "X TL × N = Y TL" görünsün.
        // Kazanç 0 ise (toggle KAPALI vb.) formül anlamsız; sadece "KAZANÇ: 0 TL" yazsın.
        if (toplamX > 1 && hamKazanc > 0)
            _ctx.CarpanFormulGosterAktivateEt(toplamX);
        else
            _ctx.CarpanKutuUcusFormulKilidiniKaldir();

        // BUG 1 fix (2026-04-29): Eskiden burada "ardışık kayıp kırıntısı" ve "min ödeme garantisi" sahte para enjekte ediyordu.
        // Sahte para tamamen kaldırıldı. Kaçış Frenleme artık BİR SONRAKİ spin'in grid'ini cluster oluşacak şekilde zorlar
        // (bkz. spin sonu güncellemesi aşağıda + OyunYoneticisi.SimuleEtVeKaydetImpl içinde GrideZorlaEnAzBirCluster çağrısı).

        // BUG FIX (2026-04-29): Maks ödeme tavanı KALDIRILDI. Görünen kazanç ile bakiyeye eklenen
        // miktar tutarsızdı (örn: 300.000 TL gösterilip 100.000 ekleniyordu) — bu oyuncuyu kandırıyor gibi
        // görünüyordu. Artık clamp yok; yalnızca negatif sanity check uygulanır.
        if (teorikToplam < 0) teorikToplam = 0;

        int odenen = 0;
        if (teorikToplam > 0)
        {
            if (_ctx.BonusAktif)
            {
                odenen = teorikToplam;
                _ctx.BonusPendingOdemeTL += odenen;
            }
            else if (zorlaCarpanKullanildi)
            {
                odenen = teorikToplam;
                _ctx.EkonomiServisi.AddWinnings(odenen, _ctx.SpinBahisTL);
            }
            else
            {
                // BUG FIX (2026-04-29): Havuz kontrolü ve odenebilirNormal clamp KALDIRILDI.
                // Eskiden: PayFromHavuz havuz yetmezse 100K (havuz × %10) döndürüyordu, sonra Mathf.Min ile clamp.
                // Bu durum "kazanç ekranında 900.000 TL göster, bakiyeye 100.000 ekle" tutarsızlığına neden oluyordu.
                // Artık tam teorikToplam ödenir; havuz tamamen bypass.
                odenen = teorikToplam;
                _ctx.EkonomiServisi.AddWinnings(odenen, _ctx.SpinBahisTL);
                Debug.Log($"[ODEME] ham={hamKazanc} carpan={toplamX} teorikToplam={teorikToplam} odenen={odenen} (havuz bypass)");
            }
            _ctx.SonSpinKazanci = odenen;
            _ctx.DonusKayitServisi?.RecordSpinResult(_ctx.SpinPrevBakiye, _ctx.EkonomiServisi.Bakiye, _ctx.SpinBahisTL, odenen);
        }
        SettledSpinDegerleriniNormalizeEt(odenen);

        // Ardışık kayıp sayacı güncelle + Kaçış Frenleme tetikleme
        if (odenen > 0)
        {
            _ctx.ArdisikKayipSayac = 0;
        }
        else
        {
            _ctx.ArdisikKayipSayac++;
            if (_ctx.ArdisikKayipLimiti > 0 && _ctx.ArdisikKayipSayac >= _ctx.ArdisikKayipLimiti)
            {
                _ctx.SonrakiSpinKacisFrenlemeAktifEt();
                _ctx.ArdisikKayipSayac = 0;
                OturumKayitcisi.EkleEvent("kacis_frenleme_aktif", $"{_ctx.ArdisikKayipLimiti} kayıp sonrası bir sonraki spin'de cluster zorlanıyor", _ctx.spinNo);
                Debug.Log($"[KAÇIŞ_FRENLEME] {_ctx.ArdisikKayipLimiti} ardışık kayıp → bir sonraki spin için cluster zorlanacak.");
            }
        }

        // Bonus otomatik tetikleme periyodu (admin paneli)
        if (_ctx.BonusOtomatikSpinPeriyodu > 0 && !_ctx.BonusAktif)
        {
            _ctx.BonusOtomatikSpinSayaci++;
            if (_ctx.BonusOtomatikSpinSayaci >= _ctx.BonusOtomatikSpinPeriyodu)
            {
                _ctx.BonusOtomatikTetikSonrakiSpin = true;
                _ctx.BonusOtomatikSpinSayaci = 0;
                Debug.Log($"[BONUS_OTOMATIK] {_ctx.BonusOtomatikSpinPeriyodu} spin sonrası bonus tetiklenecek (sonraki spin sonu).");
            }
        }

        if (_ctx.Senaryo23SpinSonrasiDonguIlerletilmeliMi(kayit))
            _ctx.Senaryo2Veya3SpinSonundaUstUsteDonguIlerlet();

        Debug.Log($"[NORMAL] SpinKazanci(istenen)={teorikToplam} Odenen={odenen} | Bakiye={_ctx.EkonomiServisi.Bakiye}");

        // BUG FIX (2026-04-29): Eski "İhlal" invariant kontrolü kaldırıldı — havuz clamp'i artık yok,
        // her spin teorik kazancın tamamını ödüyor; bu kontrol her büyük kazançta yanlış yere LogError üretirdi.

        if (_ctx.Grid != null && _ctx.Satir > 0 && _ctx.Sutun > 0)
        {
            var counts = new System.Collections.Generic.Dictionary<int, int>();
            for (int y = 0; y < _ctx.Satir; y++)
                for (int x = 0; x < _ctx.Sutun; x++)
                {
                    int v = _ctx.Grid[x, y];
                    if (v >= 0) { if (!counts.ContainsKey(v)) counts[v] = 0; counts[v]++; }
                }
            var sb = new System.Text.StringBuilder("[SPIN] Meyve dağılımı: ");
            foreach (var kv in counts)
                sb.Append($"Meyve{kv.Key}={kv.Value} ");
            Debug.Log(sb.ToString());
        }

        if (_ctx.SpinBahisTL > 0 && !_ctx.SpinKazanciOturumaEklendi)
        {
            // Oturum kazancı artık sadece bonus oyun sırasında kullanılacak.
            // Normal spinde sadece flag güncellensin.
            if (_ctx.BonusAktif && odenen > 0)
                _ctx.OturumKazanc += odenen;
            _ctx.SpinKazanciOturumaEklendi = true;
        }

        _ctx.SenaryoOdenebilirGuncelle(odenen, _ctx.SpinBahisTL);

        int[,] grid = _ctx.Grid;
        // Bonus tetiklemesi ilk düşüşteki (IlkGrid) scatter sayısına göre; scatter sayısı SpinTamamlandi'den önce bilinmeli (oturum sayacı doğru sıfırlansın).
        int scIlk = 0;
        if (kayit != null && kayit.IlkGrid != null && _ctx.IzgaraServisi != null)
            scIlk = _ctx.IzgaraServisi.ScatterSay(kayit.IlkGrid);
        int scSimdi = (_ctx.IzgaraServisi != null && grid != null) ? _ctx.IzgaraServisi.ScatterSay(grid) : 0;
        int sc = Mathf.Max(scIlk, scSimdi);
        int esik = _ctx.SenaryoServisi.GetScatterEsik();
        int scatterIdx = _ctx.IzgaraServisi != null ? _ctx.IzgaraServisi.GetScatterSpriteIndex() : -1;
        Debug.Log($"🧪 BonusKontrol: ScatterSay(ilk)={scIlk} ScatterSay(simdi)={scSimdi} kullanilan={sc} / Esik={esik} | scatter index={scatterIdx}");

        // Otomatik bonus tetikleme: önceki spin sonunda flag set edildiyse bonus başlat
        bool bonusOtomatikTetik = _ctx.BonusOtomatikTetikSonrakiSpin;
        if (bonusOtomatikTetik)
        {
            _ctx.BonusOtomatikTetikSonrakiSpin = false;
            Debug.Log("[BONUS_OTOMATIK] Periyot sonu — bonus zorla tetikleniyor.");
            SenaryoYoneticisi.I?.OnBonusTriggered();
        }
        else if (sc >= esik)
        {
            Debug.Log("🧪 BonusKontrol: EŞİK GEÇİLDİ -> Bonus başlıyor");
            SenaryoYoneticisi.I?.OnBonusTriggered();
        }
        SenaryoYoneticisi.I?.SpinTamamlandi(odenen, _ctx.SpinBahisTL);
        int bakiyeSon = _ctx.EkonomiServisi != null ? _ctx.EkonomiServisi.Bakiye : 0;
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_NormalSpinBitti, $"Normal spin tamamlandı. Ödenen: {odenen:N0} TL. Bakiye: {bakiyeSon:N0} TL.");

        if (sc < esik && grid != null && _ctx.Satir > 0 && _ctx.Sutun > 0)
        {
            var counts = new System.Collections.Generic.Dictionary<int, int>();
            for (int y = 0; y < _ctx.Satir; y++)
                for (int x = 0; x < _ctx.Sutun; x++)
                {
                    int v = grid[x, y];
                    if (v >= 0) { if (!counts.ContainsKey(v)) counts[v] = 0; counts[v]++; }
                }
            var sb = new System.Text.StringBuilder("Grid sembol dağılımı (index->adet): ");
            foreach (var kv in counts)
                sb.Append($"{kv.Key}->{kv.Value} ");
            Debug.Log(sb.ToString() + $"\n-> 4-5 silah görüyorsan, o adette olan index scatter olmalı (şu an scatterIdx={scatterIdx}). TumbleAyarlari.ScatterIndex'i Inspector'dan ayarla.");
        }

        // Finale aşamasında oyunu bilinçli olarak yavaşlat: her spin sonrası kısa duraklama
        if (SenaryoYoneticisi.I != null && SenaryoYoneticisi.I.mevcutAsama == SenaryoYoneticisi.SenaryoAsama.Asama7_Finale)
            yield return new WaitForSeconds(1.5f);

        if (sc >= esik || bonusOtomatikTetik)
        {
            if (sc >= esik && _runCoroutine != null)
                yield return _runCoroutine(_ctx.ScatterBuyutEfekti());
            // Bonus başlangıç paneli, scatter birleşme/patlama efekti tamamen algılandıktan sonra açılsın.
            yield return new WaitForSecondsRealtime(0.80f);
            _ctx.SpinCalisiyor = false;
            _ctx.BaslatBonus();
            yield break;
        }

        // Yakın Kaçırma: KAYIP spin'de (cluster yok) yakinKacirma oranıyla görsel near-miss enjekte et
        if (odenen == 0 && _ctx.YakinKacirmaDegeri10da > 0 && !_ctx.BonusAktif
            && UnityEngine.Random.Range(0, 10) < _ctx.YakinKacirmaDegeri10da)
        {
            _ctx.GrideNearMissEnjekteEt();
        }

        // Çarpan düşse de spin özeti popup'ı mutlaka açılsın.
        // Bomba animasyonu SimulasyonKaydiniOynat içinde tamamlandıktan sonra bu noktaya gelinir.
        if (_runCoroutine != null)
            yield return _runCoroutine(_ctx.ShowNormalSpinSonucPopup(odenen, _ctx.SpinBahisTL));

        _ctx.SpinCalisiyor = false;
        _ctx.UIServisi?.ButonDurumu(true);
        _ctx.SetSpinIconRotate(false);
        _ctx.UIServisi?.UI_Guncelle();

        if (_ctx.BonusAktif)
            yield break;

        // Bir sonraki normal spin'i arka planda hesapla (güncel limite göre); butona basınca anında tepki verilir.
        int sonrakiLimit = _ctx.OdemeServisi != null ? _ctx.OdemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        _ctx.StartPrecomputeNextSpin(sonrakiLimit, false);
    }

    public IEnumerator BonusDongusu()
    {
        if (_ctx == null) yield break;

        _ctx.SpinCalisiyor = true;
        _ctx.UIServisi?.ButonDurumu(false);

        while (_ctx.BonusHakKalan > 0)
        {
            _ctx.UI_CarpanSifirla();
            _ctx.SpinKazancHam = 0;
            _ctx.TumbleToplamKazanc = 0;
            _ctx.SonSpinKazanci = 0;
            _ctx.SonSpinKazancHamGoster = 0;
            _ctx.SonSpinCarpanGoster = 1;
            _ctx.SonSpinKazancToplamGoster = 0;
            _ctx.SpinKazanciOturumaEklendi = false;
            _ctx.UIServisi?.UI_Guncelle();

            // BONUS BANT CLAMP KALDIRILDI (2026-04-29): Senaryo bandı bonus'u 720 TL/10 spin gibi saçma seviyelere
            // çekiyordu. Bonus oyunu artık sınırsız simüle edilir; sadece BonusMaxOdemeTL ve havuz bütçesi tavanı uygulanır.
            int bonusLimit = int.MaxValue;
            SpinSimulasyonKaydi kayit = _ctx.SimuleEtVeKaydet(bonusLimit, true);
            if (_runCoroutine != null && kayit != null)
                yield return _runCoroutine(_ctx.SimulasyonKaydiniOynat(kayit));
            else if (kayit == null)
                yield return null;

            // Force carpan'da sisme animasyonu atla.
            bool bonusZorlaCarpan = kayit != null && kayit.ZorlaCarpanKullanildi;
            if (!bonusZorlaCarpan && _runCoroutine != null && _ctx.AnimasyonServisi != null)
                yield return _runCoroutine(_ctx.AnimasyonServisi.AnimateCarpanSisme());

            // Ödeme hesabı: ham kazanç ve çarpanı kayıttan al (simülasyon tek doğru kaynak; oynatma sonrası CarpanServisi state'i bazen senkron kalmayabiliyor, bombalar düşse bile çarpan 1 kalabiliyor).
            int hamKazanc = (kayit != null) ? kayit.ToplamHamKazanc : _ctx.SpinKazancHam;
            int toplamX = (kayit != null && kayit.NihaiCarpanToplam >= 1) ? kayit.NihaiCarpanToplam : _ctx.CarpanServisi.GetTotalMultiplierForSpin();
            int teorikToplam = _ctx.CarpanServisi.MulClampInt(hamKazanc, toplamX);
            bool zorlaCarpanGoster = kayit != null && kayit.ZorlaCarpanKullanildi;
            // BONUS BANT CLAMP KALDIRILDI (2026-04-29): senaryo bandı clamp etmiyor; sadece zorla çarpan değilse de
            // tüm teorikToplam ödenir (BonusMaxOdemeTL ve BonusBudget ileride ayrıca uygulanır).
            int maxOdenebilir = int.MaxValue;
            int spinKazanci = teorikToplam;

            _ctx.SonSpinKazancHamGoster = hamKazanc;
            _ctx.SonSpinCarpanGoster = toplamX;
            _ctx.SonSpinKazancToplamGoster = zorlaCarpanGoster ? teorikToplam : spinKazanci;
            _ctx.SonSpinKazanci = 0;
            _ctx.CarpanKutuUcusFormulKilidiniKaldir();

            int odenmekIstenen = zorlaCarpanGoster ? teorikToplam : spinKazanci;
            if (!zorlaCarpanGoster && _ctx.BonusMaxOdemeTL != int.MaxValue)
            {
                int capKalan = Mathf.Max(0, _ctx.BonusMaxOdemeTL - _ctx.BonusOdenenTL);
                odenmekIstenen = Mathf.Clamp(odenmekIstenen, 0, capKalan);
            }
            if (!zorlaCarpanGoster && _ctx.BonusBudgetAktif && _ctx.BonusBudgetKalanTL != int.MaxValue)
            {
                odenmekIstenen = Mathf.Clamp(odenmekIstenen, 0, _ctx.BonusBudgetKalanTL);
            }

            int odenen = 0;
            if (odenmekIstenen > 0)
            {
                odenen = odenmekIstenen;
                if (zorlaCarpanGoster)
                    _ctx.BonusZorlaCarpanBirikenTL += odenen;
                else
                    _ctx.BonusPendingOdemeTL += odenen;
            }

            if (odenen > 0 && !zorlaCarpanGoster) _ctx.SenaryoServisi?.RecordBonusPayment(odenen);
            _ctx.SonSpinKazanci = odenen;
            SettledSpinDegerleriniNormalizeEt(odenen);

            _ctx.DonusKayitServisi?.RecordBonusSpin(_ctx.SpinPrevBakiye, _ctx.EkonomiServisi.Bakiye, _ctx.SpinBahisTL, odenen);

            // Invariant: Bonus spinde (zorla çarpan hariç) tek spin ödemesi, bu spinin başında alınan bonusRemainingLimit'i aşmamalı.
            if (!zorlaCarpanGoster && odenen > 0 && bonusLimit != int.MaxValue && odenen > bonusLimit)
            {
                int spinNoBonus = SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : -1;
                string asamaBonus = SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.GetAsamaAdi() : "Bilinmiyor";
                Debug.LogError($"[SentetikOyuncu][İHLAL] Bonus spin ödemesi bonus limitini aştı. Odenen={odenen} TL, BonusLimit={bonusLimit} TL, SpinNo={spinNoBonus}, Asama={asamaBonus}");
            }

            if (odenen > 0 || zorlaCarpanGoster)
            {
                int oturumaEklenecek = zorlaCarpanGoster ? teorikToplam : odenen;
                _ctx.BonusKazanc += oturumaEklenecek;
                _ctx.OturumKazanc += oturumaEklenecek;
                if (odenen > 0)
                    _ctx.BonusOturumOdenenToplamTL += odenen;
                _ctx.SpinKazanciOturumaEklendi = true;
            }

            _ctx.BonusHakKalan = Mathf.Max(0, _ctx.BonusHakKalan - 1);
            _ctx.SenaryoOdenebilirGuncelle(odenen, 0);
            _ctx.UIServisi?.UI_Guncelle();

            SenaryoYoneticisi.I?.SpinTamamlandi(odenen, 0);
            SenaryoYoneticisi.I?.UI_Guncelle();

            Debug.Log($"[BONUS] SpinKazanci=" + odenen + " | BonusToplam=" + _ctx.BonusKazanc);
            if (_ctx.Grid != null && _ctx.Satir > 0 && _ctx.Sutun > 0)
            {
                var bc = new System.Collections.Generic.Dictionary<int, int>();
                for (int y = 0; y < _ctx.Satir; y++)
                    for (int x = 0; x < _ctx.Sutun; x++)
                    { int v = _ctx.Grid[x, y]; if (v >= 0) { if (!bc.ContainsKey(v)) bc[v] = 0; bc[v]++; } }
                var bs = new System.Text.StringBuilder("[SPIN] Meyve dağılımı: ");
                foreach (var kv in bc) bs.Append($"Meyve{kv.Key}={kv.Value} ");
                Debug.Log(bs.ToString());
            }

            yield return new WaitForSeconds(0.35f);
            yield return new WaitForSeconds(_ctx.BonusSpinBekleme);
        }

        // Bonus boyunca ödenen kazancı oturum sonunda tek seferde bakiyeye ekle.
        if (_ctx.BonusOturumOdenenToplamTL > 0 && _ctx.EkonomiServisi != null)
            _ctx.EkonomiServisi.AddWinnings(_ctx.BonusOturumOdenenToplamTL, 0);

        int bonusCikisBakiyesi = _ctx.EkonomiServisi != null ? _ctx.EkonomiServisi.Bakiye : 0;
        SenaryoYoneticisi.I?.LogBonusCikisi(bonusCikisBakiyesi);
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_BonusBitti, $"Bonus oyunu bitti. Toplam bonus kazanç: {_ctx.OturumKazanc:N0} TL. Çıkış bakiyesi: {bonusCikisBakiyesi:N0} TL.");
        int oturumKazanciSnapshot = _ctx.OturumKazanc;
        if (_runCoroutine != null)
            yield return _runCoroutine(_ctx.ShowBonusEndMessage(oturumKazanciSnapshot));

        _ctx.NormalOyunMusicPlay();
        _ctx.EkonomiServisi?.EkonomiSenkronizeEt();
        int spinNo = SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : 0;
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_HizHizlandi, $"Normal hıza dönüldü (bonus bitti). Spin: {spinNo}.");
        _ctx.HizVeSesServisi?.RestoreNormalSpeed();
        _ctx.NormalOyunMusicUnPause();

        _ctx.BonusAktif = false;
        SenaryoYoneticisi.I?.SetBonusAktif(false);
        _ctx.SetOturumKazancTextActive(true);
        _ctx.SpinCalisiyor = false;
        _ctx.UIServisi?.ButonDurumu(true);
        _ctx.UIServisi?.UI_Guncelle();
        _ctx.TryResumeOtomatikSpin();
    }
}
