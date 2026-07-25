// Bahis seçme paneli — bahisSec.html (Unity'nin gerçeği) iframe modal.
// bahisSec.html postMessage: {source:'yoneticiPanel', key:'bahisSec', value:miktar} / 'bahisPaneliKapat'.
let kutu = null, iframe = null, _onSec = null;

export function bahisPaneliAc(bakiye, onSec) {
  _onSec = onSec;
  if (!kutu) {
    kutu = document.createElement("div");
    Object.assign(kutu.style, { position: "absolute", left: "50%", top: "50%",
      transform: "translate(-50%,-50%)", width: "760px", height: "560px",
      zIndex: "95", pointerEvents: "auto" });
    iframe = document.createElement("iframe");
    iframe.id = "bahisSecIframe"; iframe.src = "ic/bahisSec.html";
    Object.assign(iframe.style, { width: "100%", height: "100%", border: "0", background: "transparent" });
    kutu.appendChild(iframe);
    document.getElementById("sahne").appendChild(kutu);
  }
  kutu.style.display = "block";
  // bakiyeyi panele ilet (butonlar bakiyeye göre disable)
  const gonder = () => { try { iframe.contentWindow.postMessage({ source: "unityToBahis", bakiye }, "*"); } catch (e) {} };
  setTimeout(gonder, 200); setTimeout(gonder, 600);
}

export function bahisPaneliKapat() { if (kutu) kutu.style.display = "none"; }

export function bahisKopruKur() {
  window.addEventListener("message", (e) => {
    const d = e.data;
    if (!d || d.source !== "yoneticiPanel") return;
    if (d.key === "bahisSec") { if (_onSec) _onSec(parseInt(d.value, 10)); bahisPaneliKapat(); }
    else if (d.key === "bahisPaneliKapat") bahisPaneliKapat();
  });
}
