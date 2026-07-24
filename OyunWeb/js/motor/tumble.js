// Tumble simülasyonu — CokmeAkisServisi "yerinde tumble":
// patlayan hücre boşalır, AYNI hücreye yukarıdan yeni sembol düşer (kayma YOK).
// Refill de doldurucunun ağırlıklı seçimini kullanır (RandomSymbolWithScatterChance).
import { patlayacakHucreler } from "./cluster.js";
import { kazancHesapla } from "./odeme.js";
import { sembolSecNonScatter } from "./doldurucu.js";
import { SCATTER_INDEX } from "../../veri/sabitler.js";

export const MAX_TUMBLE_TUR = 20; // OyunKorumaServisi üst sınırı

export function spinSimule(baslangicGrid, bahis, rng, secenekler = {}) {
  const { scatterSans = 0, etkinMinBoyut = 0, bias = { easy: 0, hard: 0 } } = secenekler;
  const grid = baslangicGrid.slice();
  const adimlar = [];
  let toplamHam = 0;

  for (let tur = 0; tur < MAX_TUMBLE_TUR; tur++) {
    const patlayan = patlayacakHucreler(grid);
    if (patlayan.length === 0) break;
    const kazanc = kazancHesapla(patlayan, grid, bahis, etkinMinBoyut);
    toplamHam += kazanc;
    patlayan.forEach((i) => { grid[i] = -1; }); // boşalt
    const dusen = patlayan.map((i) => {
      const s = (scatterSans > 0 && rng() < scatterSans)
        ? SCATTER_INDEX
        : sembolSecNonScatter(grid, rng, bias.easy, bias.hard);
      grid[i] = s;
      return s;
    });
    adimlar.push({ patlayan: patlayan.slice(), dusen, kazanc });
  }

  return { toplamHam, adimlar, sonGrid: grid };
}
