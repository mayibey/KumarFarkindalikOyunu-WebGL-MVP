// Ödeme eğilimi kararı — OyunYoneticisi.Admin.cs:185-244 OdemeModelineUygunMu birebir portu.
// KRİTİK (FAZ35.85 K5): Senaryolu modlarda (bant açık, aktifSenaryo != "normal") beklenen
// kazanç HEDEFİ spin başında BİR KEZ çekilir (_modSpinBekleniyorKazanc) — tüm reroll'lar
// aynı hedefi kovalar → mod eğilim yüzdesi birebir gerçekleşir. Normal modda her denemede
// zar atılır (satır 206) → oran doğal RNG ile harmanlanır (emergent).

export function bantModuMu(ayar) {
  return !!(ayar.aktifSenaryo && ayar.aktifSenaryo !== "normal" && ayar.minKat > 0 && ayar.maksKat > 0);
}

// Spin başı hedef (yalnız bant modunda anlamlı)
export function spinHedefiCek(ayar, rng) {
  return rng() <= ayar.egilimYuzde / 100;
}

export function odemeModelineUygunMu(nihaiOdeme, bahis, ayar, rng, sabitHedef = null) {
  let beklenenKazanc;
  if (bantModuMu(ayar) && sabitHedef !== null) beklenenKazanc = sabitHedef;      // :204
  else beklenenKazanc = rng() <= ayar.egilimYuzde / 100;                          // :206

  // Yontma payı kategorisi (:212-213): minKat 0<k<1 → "kazanç" = ödeme > 0
  const yontmaPayi = ayar.minKat > 0 && ayar.minKat < 1;
  const kazanc = yontmaPayi ? nihaiOdeme > 0 : nihaiOdeme > bahis;
  if (beklenenKazanc !== kazanc) return false;                                    // :214-215

  // Bant kontrolü (:224-236): yalnız kazanç spinlerinde, yalnız senaryo modlarında
  if (beklenenKazanc && kazanc && bantModuMu(ayar) && !(ayar.zorlaCarpan > 0)) {
    let minHedef = Math.round(bahis * ayar.minKat);
    let maksHedef = Math.round(bahis * ayar.maksKat);
    if (maksHedef < minHedef) [minHedef, maksHedef] = [maksHedef, minHedef];
    if (nihaiOdeme < minHedef || nihaiOdeme > maksHedef) return false;
  }

  // Tutma kayıp zorlaması (:240-241)
  if (ayar.tutmaKayipBekleniyor && nihaiOdeme > 0) return false;

  return true;
}
