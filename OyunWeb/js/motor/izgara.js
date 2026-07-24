// Izgara yardımcıları. Depolama: 1D dizi, index = y*SUTUN + x (senaryo.json ile aynı).
import { SUTUN, SATIR } from "../../veri/sabitler.js";

export const HUCRE_SAYISI = SUTUN * SATIR;
export const xyToI = (x, y) => y * SUTUN + x;
export const iToXY = (i) => [i % SUTUN, Math.floor(i / SUTUN)];

export function bosIzgara(deger = -1) {
  return new Array(HUCRE_SAYISI).fill(deger);
}
export function kopyala(g) { return g.slice(); }
