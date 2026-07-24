// Sembol doldurucu — TumbleAyarlari.RandomNonScatterSymbol:317-422 birebir portu.
// "Zengin zenginleşir" ağırlıklaması + fren mekanizması. easy/hard bias:
// ZorlukServisi:39-42 → zorluk v: easy=InverseLerp(8,4,v) (v<8), hard=InverseLerp(8,12,v) (v>8).
import { SCATTER_INDEX, MIN_CLUSTER } from "../../veri/sabitler.js";
import { aralikTam } from "./rng.js";

export function zorluktanBias(v) {
  const easy = v < 8 ? (8 - v) / 4 : 0;   // InverseLerp(8,4,v)
  const hard = v > 8 ? (v - 8) / 4 : 0;   // InverseLerp(8,12,v)
  return { easy: Math.min(1, Math.max(0, easy)), hard: Math.min(1, Math.max(0, hard)) };
}

function lerp(a, b, t) { return a + (b - a) * t; }

function biasCarpani(easyMult, hardMult, easy, hard) { // BiasMultiplier:309-315
  let m = 1;
  if (easy > 0) m *= lerp(1, easyMult, easy);
  if (hard > 0) m *= lerp(1, hardMult, hard);
  return m;
}

const N = 9; // sembol listesi uzunluğu (0..8, 8=scatter)

export function sembolSecNonScatter(grid, rng, easy = 0, hard = 0) {
  const counts = new Array(N).fill(0);
  for (const s of grid) {
    if (s < 0 || s === SCATTER_INDEX) continue;
    if (s >= 0 && s < N) counts[s]++;
  }
  let dominantIndex = -1, dominantCount = -1;
  for (let i = 0; i < N; i++) {
    if (i === SCATTER_INDEX) continue;
    if (counts[i] > dominantCount) { dominantCount = counts[i]; dominantIndex = i; }
  }
  const w = new Array(N).fill(0);
  let totalW = 0;
  for (let i = 0; i < N; i++) {
    if (i === SCATTER_INDEX) { w[i] = 0; continue; }
    let wi = 1;
    const c = counts[i];
    if (i === dominantIndex && dominantCount >= 3) wi *= biasCarpani(1.35, 1.00, easy, hard);
    if (c === MIN_CLUSTER - 4) wi *= biasCarpani(1.20, 1.00, easy, hard);
    else if (c === MIN_CLUSTER - 3) wi *= biasCarpani(1.60, 1.00, easy, hard);
    else if (c === MIN_CLUSTER - 2) wi *= biasCarpani(2.20, 0.70, easy, hard);
    else if (c >= MIN_CLUSTER - 1) wi *= biasCarpani(5.00, 0.25, easy, hard);
    wi = Math.max(wi, 0.08);
    w[i] = wi; totalW += wi;
  }
  if (totalW <= 0) {
    let fb = aralikTam(rng, 0, N);
    if (fb === SCATTER_INDEX) fb = (fb + 1) % N;
    return fb;
  }
  const agirlikliSec = () => {
    let r = rng() * totalW, picked = 0;
    for (let i = 0; i < N; i++) { r -= w[i]; if (r <= 0) { picked = i; break; } }
    return picked;
  };
  let picked = agirlikliSec();
  // Fren: cluster'ı tamamlayacaksa hardBias oranında bir kez yeniden çek (:401-416)
  if (counts[picked] >= MIN_CLUSTER - 1) {
    const frenSans = lerp(0, 0.65, hard);
    if (rng() < frenSans) picked = agirlikliSec();
  }
  if (picked === SCATTER_INDEX) picked = (picked + 1) % N;
  return picked;
}

// FillRandomAll benzeri sıralı doldurma; scatterSans: hücre başına scatter olasılığı
// (:300-307; maxScatter guard F5'te CurrentScatterChance ile ayrıntılanacak).
export function izgaraDoldur(rng, { easy = 0, hard = 0, scatterSans = 0, maxScatter = 3 } = {}) {
  const g = new Array(30).fill(-1);
  let scatterSayi = 0;
  for (let i = 0; i < 30; i++) {
    if (scatterSayi < maxScatter && scatterSans > 0 && rng() < scatterSans) {
      g[i] = SCATTER_INDEX; scatterSayi++;
    } else {
      g[i] = sembolSecNonScatter(g, rng, easy, hard);
    }
  }
  return g;
}
