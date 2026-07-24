// Anlatıcı Şerit köprüsü — PanelBridge.jslib AnlaticiPaneli* fonksiyonlarının karşılığı.
// anlatici.html DEĞİŞMEDEN kullanılır: aynı postMessage protokolü.
//   oyun → şerit: {source:'unityToAnlatici', asama, spin, hedefSpin, bakiyeNet,
//                  toplamSpin, spinNetleri[], tukenis, tukenisKapat}
//   şerit → oyun: {source:'anlaticiHtml', type:'ready'|'hoverZoom'} ve
//                 {source:'yoneticiPanel', key:'anlaticiAsamaDegis'|'anlaticiYenidenBaslat'}
// Konumlar jslib birebir: sol kenar, top 23vh; pasif 460px / hover 1300px genişlik.

let kutu = null, iframe = null, sonState = null, hazir = false;

export function anlaticiAc() {
  if (kutu) { kutu.style.display = "block"; return; }
  const sahne = document.getElementById("sahne");
  kutu = document.createElement("div");
  Object.assign(kutu.style, {
    position: "absolute", left: "12px", top: "248px",           // 23vh @1080
    width: "460px", height: "min(640px, 60%)",                   // clamp(330px,60vh,640px)
    zIndex: "100", transition: "width 180ms ease-out, height 180ms ease-out",
    pointerEvents: "auto",
  });
  iframe = document.createElement("iframe");
  iframe.id = "anlaticiPanelIframe";
  iframe.src = "ic/anlatici.html";
  Object.assign(iframe.style, { width: "100%", height: "100%", border: "0", background: "transparent" });
  kutu.appendChild(iframe);
  sahne.appendChild(kutu);
}

export function anlaticiKapat() { if (kutu) kutu.style.display = "none"; }

export function anlaticiGuncelle(state) {
  sonState = { ...state, source: "unityToAnlatici" };
  if (hazir && iframe?.contentWindow) iframe.contentWindow.postMessage(sonState, "*");
}

export function anlaticiKopruKur({ asamaDegis, yenidenBaslat }) {
  window.addEventListener("message", (e) => {
    const d = e.data;
    if (!d) return;
    if (d.source === "anlaticiHtml") {
      if (d.type === "ready") {
        hazir = true;
        if (sonState && iframe?.contentWindow) iframe.contentWindow.postMessage(sonState, "*");
      } else if (d.type === "hoverZoom") {
        if (!kutu) return;
        if (d.aktif) { kutu.style.width = "1300px"; kutu.style.height = "min(740px, 70%)"; kutu.style.zIndex = "200"; }
        else { kutu.style.width = "460px"; kutu.style.height = "min(640px, 60%)"; kutu.style.zIndex = "100"; }
      }
    } else if (d.source === "yoneticiPanel") {
      if (d.key === "anlaticiAsamaDegis") asamaDegis(parseInt(d.value, 10));
      else if (d.key === "anlaticiYenidenBaslat") yenidenBaslat();
    }
  });
}
