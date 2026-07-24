// Slot ızgara görünümü: 6x5 sembol sprite'ları + tumble animasyonları.
// Süreler Unity'den birebir: ANIM.pop=.38, ANIM.dusme=.82, ANIM.adimArasi=.18, ustOffset=120.
import { SEMBOLLER, ANIM, SUTUN, SATIR } from "../../veri/sabitler.js";
import { tween, bekle, easeOutCubic } from "../cekirdek/zamanlayici.js";

export class SlotGorunum {
  constructor(uygulama, dokular, { x, y, hucre = 150, bosluk = 12 }) {
    this.uygulama = uygulama;
    this.dokular = dokular; // ad -> PIXI.Texture
    this.hucre = hucre; this.bosluk = bosluk;
    this.kok = new PIXI.Container();
    this.kok.position.set(x, y);
    this.spriteler = new Array(SUTUN * SATIR).fill(null);
  }

  hucreKonum(i) {
    const x = i % SUTUN, y = Math.floor(i / SUTUN);
    return [x * (this.hucre + this.bosluk) + this.hucre / 2,
            y * (this.hucre + this.bosluk) + this.hucre / 2];
  }

  sembolDoku(id) { return this.dokular[SEMBOLLER[id]]; }

  gridGoster(grid) {
    for (const s of this.spriteler) if (s) s.destroy();
    for (let i = 0; i < grid.length; i++) {
      const sp = new PIXI.Sprite(this.sembolDoku(grid[i]));
      sp.anchor.set(0.5);
      const [px, py] = this.hucreKonum(i);
      sp.position.set(px, py);
      this.spriteyiBoyutla(sp);
      this.kok.addChild(sp);
      this.spriteler[i] = sp;
    }
  }

  spriteyiBoyutla(sp) {
    const oran = Math.min(this.hucre / sp.texture.width, this.hucre / sp.texture.height) * 0.94;
    sp.scale.set(oran);
    sp._tabanOlcek = oran;
  }

  // Bir tumble adımını oynat: patlayanlar küçülerek yok olur, yenileri yukarıdan düşer.
  async tumbleAdimiOynat(adim) {
    const patlayanlar = adim.patlayan.map((i) => this.spriteler[i]).filter(Boolean);
    await tween(this.uygulama, {
      sure: ANIM.pop,
      guncelle: (t) => patlayanlar.forEach((sp) => {
        sp.scale.set(sp._tabanOlcek * (1 - t));
        sp.alpha = 1 - t;
      }),
    });
    patlayanlar.forEach((sp) => sp.destroy());

    const yeniler = adim.patlayan.map((i, k) => {
      const sp = new PIXI.Sprite(this.sembolDoku(adim.dusen[k]));
      sp.anchor.set(0.5);
      const [px, py] = this.hucreKonum(i);
      sp.position.set(px, py - ANIM.ustOffset);
      sp.alpha = 0;
      this.spriteyiBoyutla(sp);
      this.kok.addChild(sp);
      this.spriteler[i] = sp;
      return { sp, py };
    });
    await tween(this.uygulama, {
      sure: ANIM.dusme,
      easing: easeOutCubic,
      guncelle: (t) => yeniler.forEach(({ sp, py }) => {
        sp.position.y = py - ANIM.ustOffset * (1 - t);
        sp.alpha = t;
      }),
    });
    await bekle(ANIM.adimArasi);
  }
}
