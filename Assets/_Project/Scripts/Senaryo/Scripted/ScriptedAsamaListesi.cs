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

        /// <summary>Resources.Load için kullanılan asset yol kökü (Resources/ kısmı atlanmış).</summary>
        public const string ResourcePath = "ScriptedSenaryo";
    }
}
