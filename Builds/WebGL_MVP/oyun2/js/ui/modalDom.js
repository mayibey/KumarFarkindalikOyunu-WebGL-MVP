// Eğitmen modalı — ScriptedModalKopru karşılığı (DOM, #sahne içinde → deck ölçeğiyle büyür).
// TMP rich text (<color=#x><b><i>) → HTML span çevirisi. Tıkla → devam (Promise).
import { bekle } from "../cekirdek/zamanlayici.js";

export function tmpToHtml(m) {
  return (m || "")
    .replace(/<color=(#[0-9a-fA-F]{6,8})>/g, '<span style="color:$1">')
    .replace(/<\/color>/g, "</span>")
    .replace(/<b>/g, "<strong>").replace(/<\/b>/g, "</strong>")
    .replace(/<i>/g, "<em>").replace(/<\/i>/g, "</em>")
    .replace(/\n/g, "<br>");
}

let _kok = null;
function kokAl() {
  if (_kok) return _kok;
  _kok = document.createElement("div");
  Object.assign(_kok.style, {
    position: "absolute", left: "0", top: "0", width: "1920px", height: "1080px",
    pointerEvents: "none", zIndex: "40", fontFamily: "'Segoe UI', Tahoma, sans-serif",
  });
  document.getElementById("sahne").appendChild(_kok);
  return _kok;
}

// Eğitmen konuşma balonu: sol-alt silüet + balon; tıklayınca kapanır.
export function egitmenModal(mesajTmp) {
  return new Promise(async (coz) => {
    const kok = kokAl();
    const kutu = document.createElement("div");
    Object.assign(kutu.style, {
      position: "absolute", left: "70px", bottom: "170px", width: "620px",
      background: "rgba(15,10,26,0.96)", border: "2px solid rgba(212,162,74,0.7)",
      borderRadius: "16px", padding: "22px 26px 18px", color: "#fff",
      fontSize: "26px", lineHeight: "1.45", pointerEvents: "auto", cursor: "pointer",
      boxShadow: "0 10px 40px rgba(0,0,0,0.6)",
    });
    const yuz = document.createElement("img");
    yuz.src = "varlik/gorsel/egitmen_yuz.webp";
    Object.assign(yuz.style, { position: "absolute", left: "-14px", top: "-64px",
      width: "92px", height: "92px", borderRadius: "50%",
      border: "3px solid #d4a24a", objectFit: "cover", background: "#000" });
    kutu.appendChild(yuz);
    const metin = document.createElement("div");
    kutu.appendChild(metin);
    const ipucu = document.createElement("div");
    ipucu.textContent = "▶ devam etmek için tıkla";
    Object.assign(ipucu.style, { marginTop: "12px", fontSize: "17px",
      color: "#d4a24a", textAlign: "right", opacity: "0" });
    kutu.appendChild(ipucu);
    kok.appendChild(kutu);

    // Typewriter (ScriptedModalKopru hissi)
    const html = tmpToHtml(mesajTmp);
    const duz = html.replace(/<[^>]+>/g, "");
    let i = 0, bitti = false;
    const yaz = setInterval(() => {
      i += 2;
      if (i >= duz.length) { clearInterval(yaz); metin.innerHTML = html; ipucu.style.opacity = "1"; bitti = true; }
      else metin.textContent = duz.slice(0, i);
    }, 24);

    kutu.addEventListener("click", async () => {
      if (!bitti) { clearInterval(yaz); metin.innerHTML = html; ipucu.style.opacity = "1"; bitti = true; return; }
      kutu.remove(); coz();
    });
  });
}
