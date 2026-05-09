using System;
using System.Collections.Generic;
using UnityEngine;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Anlatıcı sahnesinde RNG'yi bypass eden deterministik spin tanımı.
    /// Tek bir spin'i kapsar: ilk grid, çarpan dağılımı, tumble adımları, modal mesajı.
    /// </summary>
    [Serializable]
    public class ScriptedSpinKaydi
    {
        /// <summary>1-indexed (1, 2, 3, ...). Aşamadaki sıra numarası, AnlaticiSeritKopru spin sayacıyla eşleşir.</summary>
        public int spinSiraNo;

        /// <summary>0..6 (Anlatıcı aşamaları, 0 = Isındırma, 6 = Tükeniş).</summary>
        public int asamaIndex;

        /// <summary>Bu spin için bahis. Anlatıcı yeni aşama geçişinde otomatik set ettiği değerle aynı olmalı.</summary>
        public int bahis;

        /// <summary>Spin'in pedagojik tipi. UI/log/modal davranışı bu değere göre değişebilir.</summary>
        public SpinTipi tip;

        /// <summary>
        /// Bu spin için oyuncuya gösterilecek nihai brüt ödeme (TL). Çarpan SUM uygulandıktan sonraki değer
        /// SpinSimulasyonKaydi.NihaiCarpanToplam ile birlikte hesaplanır; bu alan ham (çarpansız) toplam.
        /// Plan tablosundaki "Brüt" sütunuyla eşleşir.
        /// </summary>
        public long brutOdeme;

        /// <summary>
        /// 30 hücre (6 sütun × 5 satır), 1D row-major (index = y * 6 + x).
        /// Hücre değerleri: 0..N-1 sembol, -1 boş, -2 = CARPAN_SEMBOL (üstüne <see cref="ilkCarpanDegerleri"/> bakılır).
        /// </summary>
        public int[] ilkGridSemboller;

        /// <summary>
        /// 30 hücre, her hücredeki çarpan değeri. 0 = çarpan yok. Sadece <see cref="ilkGridSemboller"/>[i] == -2
        /// olan hücrelerde anlamlıdır.
        /// </summary>
        public int[] ilkCarpanDegerleri;

        /// <summary>Sırayla oynanacak tumble adımları. Boş liste = ilk gridde patlama yok (saf sıfır / near-miss / çarpan kaçtı).</summary>
        public List<TumbleAdimTanimi> tumbleler = new List<TumbleAdimTanimi>();

        /// <summary>
        /// null/empty = mesaj yok. Doluysa spin başlamadan önce bloke eden modal panel açılır,
        /// OK basılana kadar spin akışı bekler. Aşama 4'te ScriptedModalKopru tarafından tüketilir.
        /// </summary>
        public string modalMesaji;

        /// <summary>
        /// A5 Spin 3 senaryosu: x500 çarpan grid'e düşer ama hiçbir sembolden 8'lik cluster oluşmaz,
        /// ödeme 0, görsel olarak çarpan yanıp söner ve "kaçtı" hissi yaratılır.
        /// </summary>
        public bool carpanKactiFlag;

        /// <summary>
        /// A5 Spin 4 — bonus tuzağı senaryosu. true ise normal spin akışı yerine ScriptedBonusOyunUygulayici
        /// devreye girer: oyuncunun tüm bakiyesi otomatik bonus oyuna yatırılır, cüzi <see cref="bonusGetirisi"/>
        /// döndürülür. "Değişken oranlı pekiştireç" pedagojik anı.
        /// </summary>
        public bool bonusOyunuTetikle;

        /// <summary>
        /// Bonus oyun sonu oyuncuya geri ödenen miktar (TL). Genelde yatırılanın çok altında (cüzi).
        /// </summary>
        public int bonusGetirisi;
    }

    /// <summary>
    /// Spin'in pedagojik / akış tipi. Anlatıcı modal seçimi, log etiketleri ve UI feedback için kullanılır.
    /// </summary>
    public enum SpinTipi
    {
        /// <summary>Saf rastgele sıfır — hiçbir cluster patlamaz, brüt 0.</summary>
        Sifir = 0,

        /// <summary>Cluster eşiğine 1 eksikle yaklaşılır (7 üzüm gibi); patlama olmaz, brüt 0.</summary>
        NearMiss = 1,

        /// <summary>Normal kazançlı spin; 1+ cluster patlar.</summary>
        Kazanc = 2,

        /// <summary>Yüksek çarpanlı kazanç; A1 Spin 7 (x5), A4 Spin 5 (x100) gibi vuruşlar.</summary>
        MegaWin = 3,

        /// <summary>A5 Spin 4 — bonus tuzağı modalı + bonus oyun başlatma.</summary>
        BonusTetik = 4,

        /// <summary>Bahis kadar ödeme yapan "sahte umut" spin'i (A6 Spin 6 gibi).</summary>
        BahisIadesi = 5
    }

    /// <summary>
    /// Tek bir tumble adımının tanımı (patlama + üstten düşen yeni semboller).
    ///
    /// MİMARİ NOTU (AŞAMA 3.10): Motor yer çekimi YAPMIYOR (CokmeAkisServisi.YerindeTumbleRefillGridOlustur:
    /// patlayan hücreye -1 koyar, sonra o boşluğa yeni sembol). Yani patlayan hücre **aynı yerde** yeni
    /// sembol alır. Bu yüzden tumble kaydı 30 hücreli grid kopyası tutmaya gerek yok — sadece patlayan
    /// koordinatlar + her patlayan hücre için bir "düşen sembol" yeterli.
    ///
    /// Görsel olarak yeni semboller yukarıdan düşer (AnimateOneCellWithFade); ekran "yer çekimi var" hissi
    /// verir ama mantık olarak hücre yerinde yenilenir.
    /// </summary>
    [Serializable]
    public class TumbleAdimTanimi
    {
        /// <summary>Bu adımda patlayacak hücre koordinatları (Vector2Int olarak; x=sütun, y=satır).</summary>
        public List<Vector2Int> patlayanHucreler = new List<Vector2Int>();

        /// <summary>
        /// Patlayan her hücreye düşecek yeni sembol. <see cref="patlayanHucreler"/> ile paralel; uzunluk eşit olmalı.
        /// Index i: patlayanHucreler[i] koordinatına yukaridanDusenSemboller[i] sembolü gelir.
        /// </summary>
        public int[] yukaridanDusenSemboller;

        /// <summary>
        /// Düşen her sembol için çarpan değeri (0 = çarpan yok). <see cref="yukaridanDusenSemboller"/> ile paralel.
        /// Pozitif değer → o hücrede CARPAN_SEMBOL (-2) yerleşir, çarpan değeri burada saklanır.
        /// </summary>
        public int[] yukaridanDusenCarpanlar;
    }
}
