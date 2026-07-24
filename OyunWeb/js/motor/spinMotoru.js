// Spin motoru — SimuleEtVeKaydetImpl reroll çekirdeği:
// (bant modunda hedef BİR KEZ) → grid üret → tumble simüle → eğilim uygun mu? → reroll.
import { izgaraDoldur, zorluktanBias } from "./doldurucu.js";
import { spinSimule } from "./tumble.js";
import { odemeModelineUygunMu, spinHedefiCek, bantModuMu } from "./egilim.js";

export function carpanToplamiUygula(ham, carpanToplami) {
  const etkin = carpanToplami > 0 ? carpanToplami : 1;
  return Math.min(Math.round(ham * etkin), 2147483647);
}

// ayar: { egilimYuzde, minKat, maksKat, aktifSenaryo, zorluk(4-12, default 8),
//         scatterSans, maxReroll, tutmaKayipBekleniyor }
export function spinUret(bahis, ayar, rng) {
  const maxReroll = ayar.maxReroll ?? 28;
  const bias = zorluktanBias(ayar.zorluk ?? 8);
  const sabitHedef = bantModuMu(ayar) ? spinHedefiCek(ayar, rng) : null; // FAZ35.85 K5
  let sonuc = null;
  for (let deneme = 0; deneme <= maxReroll; deneme++) {
    const grid = izgaraDoldur(rng, { easy: bias.easy, hard: bias.hard, scatterSans: ayar.scatterSans ?? 0 });
    const sim = spinSimule(grid, bahis, rng, { scatterSans: ayar.scatterSans ?? 0, bias });
    const nihai = carpanToplamiUygula(sim.toplamHam, 0); // doğal çarpan üretimi F5'te
    sonuc = { ...sim, nihai, baslangicGrid: grid, deneme, hedef: sabitHedef };
    if (odemeModelineUygunMu(nihai, bahis, ayar, rng, sabitHedef)) return sonuc;
  }
  return sonuc; // bütçe bitti: son üretilen kabul (Unity davranışı)
}
