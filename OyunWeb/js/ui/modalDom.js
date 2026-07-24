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

// Bilgilendirici Asistan — senaryo başı büyük tanıtım modalı (ScriptedModalKopru karşılığı).
// Ekran ortasında geniş kutu, "BİLGİLENDİRİCİ ASİSTAN" başlığı + zengin HTML içerik.
export function asistanModal(baslik, govdeHtml, sayfa = "") {
  return new Promise((coz) => {
    const kok = kokAl();
    const bg = document.createElement("div");
    Object.assign(bg.style, { position: "absolute", inset: "0",
      background: "rgba(0,0,0,0.5)", pointerEvents: "auto" });
    const kutu = document.createElement("div");
    Object.assign(kutu.style, { position: "absolute", left: "50%", top: "50%",
      transform: "translate(-50%,-50%)", width: "760px", maxHeight: "78%", overflow: "auto",
      background: "rgba(14,20,40,0.97)", border: "2px solid rgba(212,162,74,0.7)",
      borderRadius: "18px", padding: "26px 34px", color: "#e8ecf5",
      fontSize: "21px", lineHeight: "1.5", cursor: "pointer",
      boxShadow: "0 16px 60px rgba(0,0,0,0.7)" });
    // Öncelik 3a: sol kenarda altın çerçeveli yuvarlak avatar (Unity birebir)
    // Öncelik 4: başlık modal metin sütununa göre ORTALANMIŞ (sayfa sayacı sağda)
    kutu.style.paddingLeft = "96px";
    kutu.innerHTML = `
      <img src="varlik/gorsel/egitmen_yuz.webp" style="position:absolute;left:-46px;top:26px;
        width:88px;height:88px;border-radius:50%;border:3px solid #d4a24a;object-fit:cover;
        background:#000;box-shadow:0 6px 20px rgba(0,0,0,.6)">
      <div style="position:relative;border-bottom:1px solid rgba(212,162,74,.3);
        padding-bottom:10px;margin-bottom:16px">
        <span style="display:block;text-align:center;color:#d4a24a;font-weight:700;
          letter-spacing:2px;font-size:18px">${baslik}</span>
        <span style="position:absolute;right:0;top:0;color:#8a94a8;font-size:16px">${sayfa}</span>
      </div>
      <div>${govdeHtml}</div>
      <div style="margin-top:18px;text-align:right;color:#d4a24a;font-size:16px">▶ devam etmek için tıkla</div>`;
    bg.appendChild(kutu); kok.appendChild(bg);
    bg.addEventListener("click", () => { bg.remove(); coz(); });
  });
}

// Senaryo başı 3 karşılama modalı (03_SenaryoluOyun_Modal_Metinleri.md'den BİREBİR).
export async function karsilamaModallari() {
  await asistanModal("BİLGİLENDİRİCİ ASİSTAN",
    `Hoş geldiniz. Bu simülasyonda online kumar oyunlarının oyuncuları nasıl etkilediğini birlikte göreceğiz.
    <br><br><b>Önce oyunu tanıyalım:</b><br>
    • Ekranda 6×5'lik meyve makinesi var. SPIN tuşuna basıldığında meyveler döner.<br>
    • Aynı meyveden <span style="color:#f4d678">8 veya daha fazlası</span> bir araya gelirse kazanç verir.<br>
    • Bazı turlarda <span style="color:#f4d678">ÇARPAN</span> düşer (×2, ×5, ×100 vs.) ve kazancı katlar.<br>
    • Kazanan meyveler patlar, üstten yenileri düşer (<span style="color:#f4d678">TUMBLE</span>); zincir kazançlar olur.<br>
    • 4 Bonus Sembolü (yıldız) gelirse BONUS oyun açılır.<br><br>
    <b>Ekrandaki diğer öğeler:</b><br>
    • <b>Sol panel:</b> Oyuncunun hangi aşamada olduğunu, sahne arkasında ne yaşandığını gösterir.<br>
    • <b>Bakiye:</b> Oyuna ayrılan para (50.000 TL ile başlıyor).<br>
    • <b>Bahis:</b> Her spinde harcanacak miktar (+/− ile değişir).<br>
    • <b>KAZANÇ:</b> O spinde kazanılan miktar.<br><br>
    Hadi başlayalım: ilk aşama <i>'Isındırma ve Umut'</i>.`, "1 / 3");
  await asistanModal("BİLGİLENDİRİCİ ASİSTAN",
    `<b>İlk aşama: <i>Isındırma ve Umut</i></b><br><br>
    <span style="color:#ef4444">İlk kazanç</span>, oyuncu için en tehlikeli başlangıçtır. Beyin bu
    olumlu deneyimi güçlü biçimde hatırlar ve kişi oyunda kalmaya devam eder.<br><br>
    Uzun süreli oynama davranışının temelinde, ilk kazanmanın yarattığı bu etki bulunur.`, "2 / 3");
  await asistanModal("BİLGİLENDİRİCİ ASİSTAN",
    `<b>Şimdi deneyelim</b><br><br>
    Tam 8 spin at ve neler olduğunu görelim. Bakiyenin nasıl yükseldiğine, kazançların sıklığına dikkat edelim.<br><br>
    Sol panelde <span style="color:#f4d678">SAHNE ARKASI</span> ve
    <span style="color:#a78bfa">OYUNCUNUN KAFASI</span> bölümlerini takip edelim — sistemin gerçekte
    ne yaptığını orada göreceğiz.`, "3 / 3");
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
