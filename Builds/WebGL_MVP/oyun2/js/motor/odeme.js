// Ödeme hesabı — TumbleAyarlari.CalculateWinWithOwnPayTable:100-149 birebir portu.
import { ODEME_8_9, ODEME_10_11, ODEME_12P, MIN_CLUSTER } from "../../veri/sabitler.js";

// Unity Mathf.RoundToInt = banker's rounding (.5 çifte yuvarlar)
export function unityRound(f) {
  const alt = Math.floor(f);
  const kesir = f - alt;
  if (kesir > 0.5) return alt + 1;
  if (kesir < 0.5) return alt;
  return alt % 2 === 0 ? alt : alt + 1;
}

export function kazancHesapla(cikanHucreler, grid, bahis, etkinMinBoyut = 0) {
  if (!cikanHucreler || !grid) return 0;
  const minPay = (etkinMinBoyut > 0 && etkinMinBoyut < MIN_CLUSTER) ? etkinMinBoyut : MIN_CLUSTER;

  const sayim = new Map();
  for (const i of cikanHucreler) {
    const sym = grid[i];
    if (sym < 0) continue;
    sayim.set(sym, (sayim.get(sym) || 0) + 1);
  }

  let toplam = 0;
  for (const [sym, count] of sayim) {
    if (count < minPay) continue;
    let pay = 0;
    const kucukCarpan = (minPay <= 7 && count <= 7) ? 0.5 : 1;
    if (count <= 7 && minPay <= 7) {
      if (sym >= 0 && sym < ODEME_8_9.length) pay = ODEME_8_9[sym] * kucukCarpan;
    } else if (count <= 9) {
      if (sym >= 0 && sym < ODEME_8_9.length) pay = ODEME_8_9[sym];
    } else if (count <= 11) {
      if (sym >= 0 && sym < ODEME_10_11.length) pay = ODEME_10_11[sym];
    } else {
      if (sym >= 0 && sym < ODEME_12P.length) pay = ODEME_12P[sym];
    }
    toplam += pay * bahis;
  }
  return unityRound(toplam);
}
