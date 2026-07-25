// Slot ızgara görünümü: 6x5 sembol/çarpan hücreleri + tumble animasyonları.
// Süreler Unity'den birebir: ANIM.pop=.38, ANIM.dusme=.82, ANIM.adimArasi=.18, ustOffset=120.
// Çarpan hücresi (id=-2): bonanzabomba sprite'ı + üstünde xDEĞER metni (CarpanServisi görünümü).
import { SEMBOLLER, ANIM, SUTUN, SATIR, CARPAN_SEMBOL } from "../../veri/sabitler.js";
import { tween, bekle, easeOutCubic } from "../cekirdek/zamanlayici.js";

export class SlotGorunum {
  constructor(uygulama, dokular, { x, y, hucre = 150, boslukX = 12, boslukY = 12 }) {
    this.uygulama = uygulama;
    this.dokular = dokular;
    this.hucre = hucre; this.boslukX = boslukX; this.boslukY = boslukY;
    this.kok = new PIXI.Container();
    this.kok.position.set(x, y);
    this.hucreler = new Array(SUTUN * SATIR).fill(null); // Container'lar
  }

  hucreKonum(i) {
    const x = i % SUTUN, y = Math.floor(i / SUTUN);
    return [x * (this.hucre + this.boslukX) + this.hucre / 2,
            y * (this.hucre + this.boslukY) + this.hucre / 2];
  }

  hucreYap(id, carpanDeger = 0) {
    const c = new PIXI.Container();
    const doku = id === CARPAN_SEMBOL ? this.dokular.sembol_bomba : this.dokular[SEMBOLLER[id]];
    const sp = new PIXI.Sprite(doku);
    sp.anchor.set(0.5);
    // Sembol Unity boyutuna (biraz daha küçük, hücre içinde oturaklı)
    const oran = Math.min(this.hucre / sp.texture.width, this.hucre / sp.texture.height) * 0.94;
    sp.scale.set(oran);
    c.addChild(sp);
    if (id === CARPAN_SEMBOL && carpanDeger > 0) {
      const t = new PIXI.Text({ text: `x${carpanDeger}`, style: {
        fontFamily: "LilitaOne, TitanOne, sans-serif", fontSize: Math.round(this.hucre * 0.34),
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

  // Tüm grid'i GRID TEPESİNDEN dökerek göster (Unity açılış + her spin başı hissi).
  // Sütun sütun hafif gecikmeli akış; düşüş maske içinde görünür.
  async gridDusereksGoster(grid, carpanlar = null) {
    for (const h of this.hucreler) if (h) h.destroy({ children: true });
    const ustBaslangic = -this.hucre;
    const yeniler = [];
    for (let i = 0; i < grid.length; i++) {
      const c = this.hucreYap(grid[i], carpanlar ? carpanlar[i] : 0);
      const [px, py] = this.hucreKonum(i);
      c.position.set(px, ustBaslangic);
      this.kok.addChild(c);
      this.hucreler[i] = c;
      const sutun = i % SUTUN, satir = Math.floor(i / SUTUN);
      yeniler.push({ c, py, gecikme: sutun * 0.045 + satir * 0.02 });
    }
    await tween(this.uygulama, {
      sure: ANIM.dusme + 0.3,
      easing: easeOutCubic,
      guncelle: (t) => yeniler.forEach(({ c, py, gecikme }) => {
        const tt = Math.max(0, Math.min(1, (t - gecikme) / (1 - gecikme)));
        c.position.y = ustBaslangic + (py - ustBaslangic) * tt;
      }),
    });
  }

  async tumbleAdimiOynat(adim) {
    const patlayanlar = adim.patlayan.map((i) => this.hucreler[i]).filter(Boolean);
    await tween(this.uygulama, {
      sure: ANIM.pop,
      guncelle: (t) => patlayanlar.forEach((c) => { c.scale.set(1 - t); c.alpha = 1 - t; }),
    });
    patlayanlar.forEach((c) => c.destroy({ children: true }));

    // Meyveler GRID TEPESİNDEN dökülür (Unity hissi). Her yeni meyve grid'in üst kenarından
    // başlar → kendi hücresine iner; düşüşün TAMAMI maske içinde (görünür). Sıralı hafif gecikme
    // "akış" hissi verir. Grid üst kenarı local y = -boslukY (maske üst payıyla görünür).
    const ustBaslangic = -this.hucre;
    const yeniler = adim.patlayan.map((i, k) => {
      const id = adim.dusen[k];
      const c = this.hucreYap(id, adim.dusenCarpan ? adim.dusenCarpan[k] : 0);
      const [px, py] = this.hucreKonum(i);
      c.position.set(px, ustBaslangic);
      c.alpha = 1;
      this.kok.addChild(c);
      this.hucreler[i] = c;
      // sütuna göre küçük gecikme → soldan sağa akış hissi
      return { c, py, gecikme: (i % SUTUN) * 0.03 };
    });
    await tween(this.uygulama, {
      sure: ANIM.dusme + 0.15,
      easing: easeOutCubic,
      guncelle: (t) => yeniler.forEach(({ c, py, gecikme }) => {
        const tt = Math.max(0, Math.min(1, (t - gecikme) / (1 - gecikme)));
        c.position.y = ustBaslangic + (py - ustBaslangic) * tt;
      }),
    });
    await bekle(ANIM.adimArasi);
  }
}
