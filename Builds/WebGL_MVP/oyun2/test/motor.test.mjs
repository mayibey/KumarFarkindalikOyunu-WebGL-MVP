// node --test — motor birim + istatistik + senaryo tutarlılık testleri.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const KOK = dirname(fileURLToPath(import.meta.url));
const { patlayacakHucreler } = await import("../js/motor/cluster.js");
const { kazancHesapla, unityRound } = await import("../js/motor/odeme.js");
const { spinUret } = await import("../js/motor/spinMotoru.js");
const { rngYap } = await import("../js/motor/rng.js");
const { MODLAR, SCATTER_INDEX } = await import("../veri/sabitler.js");

const senaryo = JSON.parse(readFileSync(join(KOK, "../veri/senaryo.json"), "utf8"));

test("unityRound banker's rounding", () => {
  assert.equal(unityRound(2.5), 2);
  assert.equal(unityRound(3.5), 4);
  assert.equal(unityRound(2.4), 2);
  assert.equal(unityRound(2.6), 3);
});

test("cluster: 8 aynı sembol patlar, 7 patlamaz, scatter asla", () => {
  const g = new Array(30).fill(-1);
  for (let i = 0; i < 8; i++) g[i] = 3;          // 8 hindistancevizi
  for (let i = 8; i < 15; i++) g[i] = 5;          // 7 muz (yetmez)
  for (let i = 15; i < 25; i++) g[i] = SCATTER_INDEX; // 10 scatter (hariç)
  const p = patlayacakHucreler(g);
  assert.equal(p.length, 8);
  assert.ok(p.every((i) => g[i] === 3));
});

test("ödeme: 8'li armut = 0.2x, 12'li üzüm = 25x, karışık toplam", () => {
  const g = new Array(30).fill(-1);
  for (let i = 0; i < 8; i++) g[i] = 0;   // 8 armut
  for (let i = 8; i < 20; i++) g[i] = 7;  // 12 üzüm
  const hucreler = [...Array(20).keys()];
  // 0.2*1000 + 25*1000 = 200 + 25000
  assert.equal(kazancHesapla(hucreler, g, 1000), 25200);
});

test("ödeme: küçük cluster 0.5x kuralı (etkinMin 6 ile 6'lı çilek)", () => {
  const g = new Array(30).fill(-1);
  for (let i = 0; i < 6; i++) g[i] = 1; // 6 çilek
  // ODEME_8_9[1]=0.3 → 0.3*0.5*1000 = 150
  assert.equal(kazancHesapla([0, 1, 2, 3, 4, 5], g, 1000, 6), 150);
});

test("senaryo.json tutarlılık: patlayan hücreler tek sembol ve >=8 (scripted kural)", () => {
  let uyumlu = 0, kuralDisi = 0;
  for (const asama of senaryo.asamaSpinleri) for (const sp of asama) {
    if (!sp.tumbleler.length) continue;
    const grid = sp.grid.slice();
    for (const t of sp.tumbleler) {
      const semboller = new Set(t.patlayan.map(([x, y]) => grid[y * 6 + x]));
      const tekSembol = semboller.size === 1;
      if (tekSembol && t.patlayan.length >= 8) uyumlu++; else kuralDisi++;
      t.patlayan.forEach(([x, y], k) => { grid[y * 6 + x] = t.dusenSemboller[k]; });
    }
  }
  console.log(`  scripted tumble adımları: kural-uyumlu=${uyumlu}, kurgu(near-miss vb)=${kuralDisi}`);
  assert.ok(uyumlu > 0);
});

test("hook modu: sabit hedef sayesinde ~%90 kazanç + bant 1.1-2.2", () => {
  const rng = rngYap(4242);
  const ayar = { egilimYuzde: MODLAR.hook.egilim, minKat: MODLAR.hook.min,
                 maksKat: MODLAR.hook.maks, aktifSenaryo: "hook", maxReroll: 2000 };
  let kazanan = 0, bantIci = 0;
  const N = 1500, bahis = 1000;
  for (let i = 0; i < N; i++) {
    const s = spinUret(bahis, ayar, rng);
    if (s.nihai > bahis) {
      kazanan++;
      if (s.nihai >= 1100 && s.nihai <= 2200) bantIci++;
    }
  }
  const oran = (kazanan / N) * 100;
  console.log(`  hook: kazanan %${oran.toFixed(1)} (hedef ~90), bant içi ${bantIci}/${kazanan}`);
  assert.ok(Math.abs(oran - 90) < 5, `oran %${oran.toFixed(1)}`);
  assert.ok(kazanan === 0 || bantIci / kazanan > 0.9);
});

test("koruma modu: ~%8 ödeyen + bant 0.1-0.3 (yontma payı: ödeme>0 kazançtır)", () => {
  const rng = rngYap(777);
  const ayar = { egilimYuzde: MODLAR.koruma.egilim, minKat: MODLAR.koruma.min,
                 maksKat: MODLAR.koruma.maks, aktifSenaryo: "koruma", maxReroll: 2000 };
  let odeyen = 0, bantIci = 0;
  const N = 2000, bahis = 1000;
  for (let i = 0; i < N; i++) {
    const s = spinUret(bahis, ayar, rng);
    if (s.nihai > 0) {
      odeyen++;
      if (s.nihai >= 100 && s.nihai <= 300) bantIci++;
    }
  }
  const oran = (odeyen / N) * 100;
  console.log(`  koruma: ödeyen %${oran.toFixed(1)} (hedef ~8), bant içi ${bantIci}/${odeyen}`);
  assert.ok(Math.abs(oran - 8) < 3);
  assert.ok(odeyen === 0 || bantIci / odeyen > 0.9);
});

test("normal mod: deneme-başı zar → emergent oran (bilgi amaçlı ölçüm + zorluk etkisi)", () => {
  const bahis = 1000, N = 4000;
  const olc = (zorluk, tohum) => {
    const rng = rngYap(tohum);
    const ayar = { egilimYuzde: 65, minKat: 0, maksKat: 0, aktifSenaryo: "normal",
                   maxReroll: 200, zorluk };
    let k = 0;
    for (let i = 0; i < N; i++) if (spinUret(bahis, ayar, rng).nihai > bahis) k++;
    return (k / N) * 100;
  };
  const notr = olc(8, 1), kolay = olc(4, 2), zor = olc(12, 3);
  console.log(`  normal emergent: zorluk8=%${notr.toFixed(1)} zorluk4=%${kolay.toFixed(1)} zorluk12=%${zor.toFixed(1)}`);
  assert.ok(kolay > notr, "kolay bias kazanç oranını artırmalı");
  assert.ok(zor <= notr + 1, "zor bias kazancı artırmamalı");
});
