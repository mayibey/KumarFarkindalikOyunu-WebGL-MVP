// Cluster tespiti — TumbleServisi.FindClustersToRemove:36 birebir portu.
// Kural: KOMŞULUK YOK; grid GENELİNDE aynı sembolden >= minSize varsa o sembolün
// TÜM hücreleri patlar. Scatter ve negatif hücreler (boş/çarpan) hariç.
import { SCATTER_INDEX, MIN_CLUSTER } from "../../veri/sabitler.js";

export function patlayacakHucreler(grid, minSize = MIN_CLUSTER) {
  const sayim = new Map(); // sembol -> hücre indexleri
  for (let i = 0; i < grid.length; i++) {
    const s = grid[i];
    if (s < 0 || s === SCATTER_INDEX) continue;
    if (!sayim.has(s)) sayim.set(s, []);
    sayim.get(s).push(i);
  }
  const cikar = [];
  for (const [, hucreler] of sayim) {
    if (hucreler.length >= minSize) cikar.push(...hucreler);
  }
  return cikar; // 1D indexler
}
