// WinFeedback — WinFeedbackUI.cs karşılığı: karartma (0.75) + dönen ışın katmanları
// (CW yavaş + CCW hızlı, ADD blend) + başlık + sayaç. Eşikler sabitler.WIN_FEEDBACK.
import { WIN_FEEDBACK } from "../../veri/sabitler.js";
import { tween, bekle, smoothStep } from "../cekirdek/zamanlayici.js";
import { sesCal } from "../cekirdek/ses.js";

const W = 1920, H = 1080;

export class WinFeedback {
  constructor(uygulama) {
    this.uygulama = uygulama;
    this.kok = new PIXI.Container();
    this.kok.visible = false;

    this.karartma = new PIXI.Graphics().rect(0, 0, W, H).fill({ color: 0x000000, alpha: 0.75 });
    this.kok.addChild(this.karartma);

    this.isinlar = [this.isinKatmani(14, 0xffd700, 0.5), this.isinKatmani(10, 0xfff3b0, 0.35)];
    this.isinlar.forEach((k) => { k.position.set(W / 2, H / 2 - 60); this.kok.addChild(k); });

    this.baslik = new PIXI.Text({ text: "", style: {
      fontFamily: "LilitaOne, TitanOne, sans-serif", fontSize: 110, fill: 0xffd700,
      stroke: { color: 0x7a4a00, width: 10 }, dropShadow: { distance: 6, alpha: 0.6 } } });
    this.baslik.anchor.set(0.5);
    this.baslik.position.set(W / 2, H / 2 - 120);
    this.kok.addChild(this.baslik);

    this.tutar = new PIXI.Text({ text: "", style: {
      fontFamily: "LilitaOne, TitanOne, sans-serif", fontSize: 88, fill: 0xffffff,
      stroke: { color: 0x000000, width: 8 } } });
    this.tutar.anchor.set(0.5);
    this.tutar.position.set(W / 2, H / 2 + 20);
    this.kok.addChild(this.tutar);

    this._donme = (tk) => {
      if (window.__uyandir) window.__uyandir();  // kazanç şovu boyunca render uyanık
      const dt = tk.deltaMS / 1000;
      this.isinlar[0].rotation += dt * 0.35;   // CW yavaş
      this.isinlar[1].rotation -= dt * 0.9;    // CCW hızlı
    };
  }

  isinKatmani(adet, renk, alfa) {
    const g = new PIXI.Graphics();
    for (let i = 0; i < adet; i++) {
      const a0 = (i / adet) * Math.PI * 2, a1 = a0 + (Math.PI * 2 / adet) * 0.45;
      g.moveTo(0, 0).arc(0, 0, 1000, a0, a1).lineTo(0, 0).fill({ color: renk, alpha: alfa });
    }
    g.blendMode = "add";
    return g;
  }

  seviye(kazanc, bahis) {
    const k = kazanc / bahis;
    if (k >= WIN_FEEDBACK.esikler.epic) return "epic";
    if (k >= WIN_FEEDBACK.esikler.mega) return "mega";
    if (k >= WIN_FEEDBACK.esikler.big) return "big";
    return null;
  }

  async goster(kazanc, bahis) {
    const sv = this.seviye(kazanc, bahis);
    if (!sv) return;
    this.baslik.text = WIN_FEEDBACK.metinler[sv];
    this.kok.visible = true;
    this.kok.alpha = 0;
    this.uygulama.ticker.add(this._donme);
    sesCal("alkis", { ses: 0.8 });

    await tween(this.uygulama, { sure: WIN_FEEDBACK.girisSn, guncelle: (t) => { this.kok.alpha = t; } });

    // Sayaç: 0 → kazanç (tik sesli)
    const sayacSure = 1.2;
    let sonTik = 0;
    await tween(this.uygulama, { sure: sayacSure, easing: smoothStep, guncelle: (t, ham) => {
      this.tutar.text = Math.round(kazanc * t).toLocaleString("tr-TR") + " TL";
      if (ham - sonTik > 0.07) { sonTik = ham; sesCal("sayac_tik", { ses: 0.4, pitchRasgele: true }); }
    } });
    await bekle(WIN_FEEDBACK.saymaFazlaSn);

    await tween(this.uygulama, { sure: WIN_FEEDBACK.cikisSn, guncelle: (t) => { this.kok.alpha = 1 - t; } });
    this.kok.visible = false;
    this.uygulama.ticker.remove(this._donme);
  }
}
