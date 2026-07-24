// Scripted senaryo oynatıcı — ScriptedSpinYoneticisi/Uygulayici karşılığı.
// senaryo.json'daki deterministik kayıtları oynatılabilir adımlara çevirir.
// Ödeme, Unity'deki gibi RUNTIME paytable ile hesaplanır (kayıtta ödeme yok).
import { kazancHesapla } from "./odeme.js";
import { xyToI } from "./izgara.js";
import { carpanToplamiUygula } from "./spinMotoru.js";

let _veri = null;
export async function senaryoVerisiYukle() {
  if (!_veri) _veri = await (await fetch("veri/senaryo.json")).json();
  return _veri;
}

export function scriptedSpinBul(asama, spinSira) {
  const liste = _veri?.asamaSpinleri?.[asama];
  return liste?.find((s) => s.sira === spinSira) || null;
}
export function asamaScriptedSpinSayisi(asama) {
  return _veri?.asamaSpinleri?.[asama]?.length || 0;
}

// Kaydı oynatma planına çevir: her tumble adımının kazancı + çarpan toplamı + nihai.
export function kaydiPlanla(kayit) {
  const grid = kayit.grid.slice();
  const carpanlar = kayit.carpanlar.slice();
  const adimlar = [];
  let ham = 0;

  for (const t of kayit.tumbleler) {
    const idx = t.patlayan.map(([x, y]) => xyToI(x, y));
    const kazanc = kazancHesapla(idx, grid, kayit.bahis);
    ham += kazanc;
    const dusen = t.dusenSemboller.slice();
    const dusenCarpan = t.dusenCarpanlar.slice();
    adimlar.push({ patlayan: idx, dusen, dusenCarpan, kazanc });
    idx.forEach((i, k) => { grid[i] = dusen[k]; carpanlar[i] = dusenCarpan[k] || 0; });
  }

  // Çarpan SUM: zincir sonunda tahtada duran çarpan hücrelerinin toplamı (Sweet Bonanza).
  let carpanToplam = 0;
  for (let i = 0; i < grid.length; i++) if (grid[i] === -2) carpanToplam += carpanlar[i] || 0;
  const nihai = ham > 0 ? carpanToplamiUygula(ham, carpanToplam) : 0;

  return { baslangicGrid: kayit.grid.slice(), baslangicCarpan: kayit.carpanlar.slice(),
           adimlar, ham, carpanToplam, nihai, kayit };
}
