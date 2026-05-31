using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// FAZ35.84: SpinAnaliz debug log aracı — her spin tek satırda detaylı analiz.
// Sahne bağımsız (her sahnede çalışır — yeni 03 admin testi öncelikli, ileride #if UNITY_EDITOR gate'lenebilir).
// Çağrı noktası: DonusAkisServisi spin sonu (NormalSpinAkisi içinde, kazanç ödeme bitince).
public partial class OyunYoneticisi
{
    public void SpinAnalizLog(int oncekiBakiye, int sonrakiBakiye, int kazanc, int bahis)
    {
        _spinAnalizNo++;
        string sahne = SceneManager.GetActiveScene().name;
        string mod = PanelKopru.aktifSenaryo ?? "normal";
        string meyveler = GridDagilimiOzet();
        string bant = (odemeMinKat > 0f && odemeMaksKat > 0f)
            ? $"{odemeMinKat}x-{odemeMaksKat}x"
            : "-";
        string tutma = _tutmaModAktif
            ? _tutmaModSpinSayac.ToString()
            : "-";
        int carpan = sonSpinCarpanGoster;
        int delta = sonrakiBakiye - oncekiBakiye;
        string deltaStr = (delta >= 0 ? "+" : "") + delta;
        string reroll = _sonSpinRerollSayisi >= 0
            ? _sonSpinRerollSayisi.ToString()
            : "scripted/konstrukte";

        Debug.Log($"[SPIN #{_spinAnalizNo} | {sahne}] Mod={mod} | " +
                  $"Bakiye:{oncekiBakiye}→{sonrakiBakiye} ({deltaStr}) | " +
                  $"Bahis:{bahis} | Meyveler:[{meyveler}] | Carpan:{carpan} | " +
                  $"Bant:{bant} | Eğilim:{_odemeEgilimiYuzde}% | " +
                  $"TutmaSayac:{tutma} | Reroll:{reroll}");
    }

    private string GridDagilimiOzet()
    {
        if (grid == null) return "grid-null";
        var dict = new Dictionary<int, int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            {
                int s = grid[x, y];
                if (s < 0) continue;
                if (!dict.ContainsKey(s)) dict[s] = 0;
                dict[s]++;
            }
        var sirali = dict.OrderByDescending(kv => kv.Value);
        return string.Join(" ", sirali.Select(kv => $"i{kv.Key}:{kv.Value}"));
    }
}
