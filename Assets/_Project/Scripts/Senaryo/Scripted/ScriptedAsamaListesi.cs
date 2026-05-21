using System;
using System.Collections.Generic;
using UnityEngine;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Tek bir aşamanın spin listesi. Unity nested generic List&lt;List&lt;T&gt;&gt; serialize edemediği için
    /// ScriptableObject içinde wrapper olarak kullanılır.
    /// </summary>
    [Serializable]
    public class AsamaSpinListesi
    {
        public List<ScriptedSpinKaydi> spinler = new List<ScriptedSpinKaydi>();
    }

    /// <summary>
    /// Anlatıcı sahnesinin 7 aşamasına karşılık gelen scripted spin senaryosu.
    /// Inspector'dan elle doldurmaya gerek yok; <c>Tools/Kumar/Scripted Senaryo Asset'ini Yeniden Üret</c>
    /// menüsü çalıştırılınca plandaki tablodan otomatik üretilir.
    /// </summary>
    [CreateAssetMenu(fileName = "ScriptedSenaryo", menuName = "Kumar/Scripted Senaryo")]
    public class ScriptedAsamaListesi : ScriptableObject
    {
        /// <summary>
        /// 7 elemanlı liste (0 = Isındırma, 6 = Tükeniş). Her aşama kendi spin tablosunu tutar.
        /// A6 dinamik (runtime üretilir), A7 cutscene (spinsiz) — bu iki aşama boş liste olur.
        /// </summary>
        public List<AsamaSpinListesi> asamaSpinleri = new List<AsamaSpinListesi>();

        /// <summary>
        /// A5 cazip popup → bonus oyun için 10 sabit scripted spin. Toplam tam 4000 TL ödeme garantili
        /// (paytable matematik doğrulanmış, bkz ScriptedSenaryoAssetUreteci.DoldurBonusSpinleri).
        /// Pedagojik ritim: 5 sıfır + 5 kazanç (anti-climax kapanış). Bonus motor RTP devre dışı,
        /// DonusAkisServisi.BonusDongusu bu kayıttan grid+tumble yükler. Her oturumda aynı pedagojik
        /// deneyim → "kumar şans" gerçeği tutarlı.
        ///
        /// FAZ 35.38 — brutOdeme alanının runtime semantiği:
        ///   • 03 Tiyatro (ScriptedSpinUygulayici): brutOdeme OKUNMAZ. Paytable hesabı runtime'da
        ///     yeniden yapılır (TumbleAyarlari.CalculateWinWithOwnPayTable). Asset'teki değer sadece
        ///     dokümantasyon/log amaçlıdır; ScriptedSenaryoAssetUreteci'de Brut() helper'ı ile aynı
        ///     paytable kullanılarak hesaplanır → asset rakamı runtime ile %100 senkron kalır.
        ///   • 04 Tutorial (TutorialScriptedYoneticisi:152): brutOdeme OKUNUR ve carpan SUM ile
        ///     çarpılır (SonOdeme = kayit.brutOdeme × carpanToplam). Tutorial pattern'leri için
        ///     brutOdeme ham (çarpansız) yazılır — Tiyatro convention'undan farklı.
        /// </summary>
        public List<ScriptedSpinKaydi> bonusSpinleri = new List<ScriptedSpinKaydi>();

        /// <summary>Resources.Load için kullanılan asset yol kökü (Resources/ kısmı atlanmış).</summary>
        public const string ResourcePath = "ScriptedSenaryo";
    }
}
