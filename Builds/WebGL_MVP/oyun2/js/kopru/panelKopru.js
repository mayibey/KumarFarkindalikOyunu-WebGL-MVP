// Manipülasyon paneli köprüsü — PanelBridge.jslib PaneliAcSolKenar + PanelKopru.AyarAl karşılığı.
// panel.html DEĞİŞMEDEN kullanılır: {source:'yoneticiPanel', key, value} → ayar objesini günceller.
import { MODLAR } from "../../veri/sabitler.js";

// Motor ayar durumu (spinUret'e verilen). Panel bunu canlı günceller.
export const panelAyar = {
  egilimYuzde: MODLAR.normal.egilim, minKat: 0, maksKat: 0,
  aktifSenaryo: "normal", zorluk: 8, maxReroll: 500, scatterSans: 0,
  carpanZorla: 0, carpanOlasilik: 2, maxCarpanAdet: 5,
};

let iframe = null, kutu = null;

export function panelAc() {
  if (kutu) { kutu.style.display = "block"; return; }
  kutu = document.createElement("div");
  Object.assign(kutu.style, { position: "absolute", left: "0", top: "50%",
    transform: "translateY(-50%)", width: "520px", height: "720px",
    zIndex: "90", pointerEvents: "auto" });
  iframe = document.createElement("iframe");
  iframe.id = "panelIframe"; iframe.src = "ic/panel.html";
  Object.assign(iframe.style, { width: "100%", height: "100%", border: "0", background: "transparent" });
  kutu.appendChild(iframe);
  (document.getElementById("domUst") || document.getElementById("sahne")).appendChild(kutu);
}
export function panelKapat() { if (kutu) kutu.style.display = "none"; }
export function panelAcikMi() { return !!kutu && kutu.style.display !== "none"; }

// PanelKopru.AyarAl(json) — shim üzerinden gelir. key/value ile motor ayarını uygula.
export function panelAyarIsle(json) {
  let d; try { d = JSON.parse(json); } catch { return; }
  const key = d.key, value = d.value;
  switch (key) {
    case "oyunModu": {
      const m = MODLAR[value] || MODLAR.normal;
      panelAyar.aktifSenaryo = value;
      panelAyar.egilimYuzde = m.egilim;
      panelAyar.minKat = m.min; panelAyar.maksKat = m.maks;
      break;
    }
    case "minCarpan": panelAyar.minKat = parseFloat(value) || 0; break;
    case "maksCarpan": panelAyar.maksKat = parseFloat(value) || 0; break;
    case "carpanOlasilik": panelAyar.carpanOlasilik = parseFloat(value) || 0; break;
    case "maxCarpanTekSpin": panelAyar.maxCarpanAdet = parseInt(value, 10) || 5; break;
    case "carpanZorla": panelAyar.carpanZorla = parseInt(value, 10) || 0; break;
    case "scatterBaskila": panelAyar.scatterSans = value === "true" ? 0 : panelAyar.scatterSans; break;
    case "paneliKapat": panelKapat(); break;
    default: break; // diğer key'ler F6 ikinci turda
  }
  console.log(`[panel] ${key} = ${value} → ayar`, { ...panelAyar });
}

// Panel'e mevcut ayarları geri gönder (panel.html "ready" beklediğinde)
export function panelKopruKur() {
  window.addEventListener("message", (e) => {
    const d = e.data;
    if (d?.source === "yoneticiPanel" && d.key === "panelHazir" && iframe?.contentWindow) {
      iframe.contentWindow.postMessage({ source: "unityToPanel", mevcutAyarlar:
        JSON.stringify(panelAyar) }, "*");
    }
  });
}
