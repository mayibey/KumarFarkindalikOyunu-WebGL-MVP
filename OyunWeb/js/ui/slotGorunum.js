// Slot ızgara görünümü: 6x5 sembol/çarpan hücreleri + tumble animasyonları.
// Süreler Unity'den birebir: ANIM.pop=.38, ANIM.dusme=.82, ANIM.adimArasi=.18, ustOffset=120.
// Çarpan hücresi (id=-2): bonanzabomba sprite'ı + üstünde xDEĞER metni (CarpanServisi görünümü).
import { SEMBOLLER, ANIM, SUTUN, SATIR, CARPAN_SEMBOL } from "../../veri/sabitler.js";
import { tween, bekle, easeOutCubic } from "../cekirdek/zamanlayici.js";

export class SlotGorunum {
  constructor(uygulama, dokular, { x, y, hucre = 150, bosluk = 12 }) {
    this.uygulama = uygulama;
    this.dokular = dokular;
    this.hucre = hucre; this.bosluk = bosluk;
    this.kok = new PIXI.Container();
    this.kok.position.set(x, y);
    this.hucreler = new Array(SUTUN * SATIR).fill(null); // Container'lar
  }

  hucreKonum(i) {
    const x = i % SUTUN, y = Math.floor(i / SUTUN);
    return [x * (this.hucre + this.bosluk) + this.hucre / 2,
            y * (this.hucre + this.bosluk) + this.hucre / 2];
  }

  hucreYap(id, carpanDeger = 0) {
    const c = new PIXI.Container();
    const doku = id === CARPAN_SEMBOL ? this.dokular.sembol_bomba : this.dokular[SEMBOLLER[id]];
    const sp = new PIXI.Sprite(doku);
    sp.anchor.set(0.5);
    const oran = Math.min(this.hucre / sp.texture.width, this.hucre / sp.texture.height) * 0.94;
    sp.scale.set(oran);
    c.addChild(sp);
    if (id === CARPAN_SEMBOL && carpanDeger > 0) {
      const t = new PIXI.Text({ text: `x${carpanDeger}`, style: {
        fontFamily: "LilitaOne", fontSize: Math.round(this.hucre * 0.34),
        fill: 0xffe14d, stroke: { color: 0x481207, width: 6 } } });
      t.anchor.set(0.5);
      t.position.set(0, 2);
      c.addChild(t);
    }
    c._tabanOlcek = 1;
    return c;
  }

  gridGoster(grid, carpanlar = null) {
    for (const h of this.hucreler) if (h) h.destroy({ children: true });
    for (let i = 0; i < grid.length; i++) {
      const c = this.hucreYap(grid[i], carpanlar ? carpanlar[i] : 0);
      const [px, py] = this.hucreKonum(i);
      c.position.set(px, py);
      this.kok.addChild(c);
      this.hucreler[i] = c;
    }
  }

  async tumbleAdimiOynat(adim) {
    const patlayanlar = adim.patlayan.map((i) => this.hucreler[i]).filter(Boolean);
    await tween(this.uygulama, {
      sure: ANIM.pop,
      guncelle: (t) => patlayanlar.forEach((c) => { c.scale.set(1 - t); c.alpha = 1 - t; }),
    });
    patlayanlar.forEach((c) => c.destroy({ children: true }));

    const yeniler = adim.patlayan.map((i, k) => {
      const id = adim.dusen[k];
      const c = this.hucreYap(id, adim.dusenCarpan ? adim.dusenCarpan[k] : 0);
      const [px, py] = this.hucreKonum(i);
      c.position.set(px, py - ANIM.ustOffset);
      c.alpha = 0;
      this.kok.addChild(c);
      this.hucreler[i] = c;
      return { c, py };
    });
    await tween(this.uygulama, {
      sure: ANIM.dusme,
      easing: easeOutCubic,
      guncelle: (t) => yeniler.forEach(({ c, py }) => {
        c.position.y = py - ANIM.ustOffset * (1 - t);
        c.alpha = t;
      }),
    });
    await bekle(ANIM.adimArasi);
  }
}
