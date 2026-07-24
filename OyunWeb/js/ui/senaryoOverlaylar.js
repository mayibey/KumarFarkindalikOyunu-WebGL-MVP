// Senaryonun dramatik DOM overlay'leri — Scripted* sınıflarının karşılıkları.
// Hepsi #sahne içinde (1920x1080), deck ölçeğiyle büyür. TMP metinleri birebir kaynaktan.
import { tmpToHtml } from "./modalDom.js";

const YESILAY = "115";

function kok() {
  let k = document.getElementById("senaryoOverlayKok");
  if (!k) {
    k = document.createElement("div");
    k.id = "senaryoOverlayKok";
    Object.assign(k.style, { position: "absolute", left: "0", top: "0",
      width: "1920px", height: "1080px", pointerEvents: "none", zIndex: "60",
      fontFamily: "'Segoe UI', Tahoma, sans-serif" });
    document.getElementById("sahne").appendChild(k);
  }
  return k;
}

function karartma() {
  const d = document.createElement("div");
  Object.assign(d.style, { position: "absolute", inset: "0",
    background: "rgba(0,0,0,0.82)", pointerEvents: "auto" });
  return d;
}

// ScriptedBonusTuzagiPopup — A4S4 cazip altın popup "10.000 KAT KAZAN".
export function bonusTuzagiPopup() {
  return new Promise((coz) => {
    const k = kok(); const bg = karartma(); k.appendChild(bg);
    const kutu = document.createElement("div");
    Object.assign(kutu.style, { position: "absolute", left: "50%", top: "50%",
      transform: "translate(-50%,-50%)", width: "620px", padding: "40px 48px",
      textAlign: "center", borderRadius: "24px", pointerEvents: "auto",
      background: "linear-gradient(135deg,#2d1810,#4a2818,#2d1810)",
      border: "4px solid #FFD700", boxShadow: "0 0 70px rgba(255,215,0,0.55)", color: "#fff" });
    kutu.innerHTML = `
      <div style="font-size:34px;font-weight:800;color:#FFD700;letter-spacing:1px">🎰 BÜYÜK FIRSAT 🎰</div>
      <div style="font-size:58px;font-weight:900;color:#FFE14D;margin:14px 0;
        text-shadow:0 3px 10px rgba(0,0,0,.6)">10.000 KAT<br>KAZAN!</div>
      <div style="font-size:22px;line-height:1.5;color:#ffe9c0;margin-bottom:26px">
        Bonus oyununu satın al, dev kazançların kapısını arala.<br>
        <span style="color:#FFD700;font-weight:700">Kaçırma!</span></div>`;
    const btn = document.createElement("button");
    btn.textContent = "BONUS AL";
    Object.assign(btn.style, { fontSize: "28px", fontWeight: "800", padding: "16px 54px",
      border: "0", borderRadius: "14px", cursor: "pointer", color: "#3a1500",
      background: "linear-gradient(180deg,#ffe14d,#f0a800)",
      boxShadow: "0 8px 22px rgba(240,168,0,.5)" });
    btn.onclick = () => { bg.remove(); coz(); };
    kutu.appendChild(btn); k.appendChild(kutu);
  });
}

// ScriptedYuklemePaneli — A5→A6 bloke panel: "Borç al" → +50.000 TL.
export function borcPaneli() {
  return new Promise((coz) => {
    const k = kok(); const bg = karartma(); k.appendChild(bg);
    const kutu = document.createElement("div");
    Object.assign(kutu.style, { position: "absolute", left: "50%", top: "50%",
      transform: "translate(-50%,-50%)", width: "600px", padding: "38px 44px",
      textAlign: "center", borderRadius: "20px", pointerEvents: "auto",
      background: "#15100a", border: "3px solid #b45309", color: "#fff" });
    kutu.innerHTML = `
      <div style="font-size:30px;font-weight:800;color:#f59e0b">BAKİYENİZ BİTTİ</div>
      <div style="font-size:21px;line-height:1.55;color:#e8d4b8;margin:18px 0 26px">
        Oyuna devam etmek için paraya ihtiyacın var.<br>
        <span style="color:#ef4444;font-weight:700">Borç alarak</span> bir şans daha
        deneyebilirsin…</div>`;
    const btn = document.createElement("button");
    btn.textContent = "BORÇ AL (+50.000 ₺)";
    Object.assign(btn.style, { fontSize: "24px", fontWeight: "800", padding: "14px 40px",
      border: "0", borderRadius: "12px", cursor: "pointer", color: "#fff",
      background: "linear-gradient(180deg,#dc2626,#991b1b)" });
    btn.onclick = () => { bg.remove(); coz(); };
    kutu.appendChild(btn); k.appendChild(kutu);
  });
}

// ScriptedFinalEkrani — A7 tükeniş cutscene (metinler birebir kaynaktan).
export function finalEkrani({ toplamYatirim, sonBakiye, netKayip, toplamSpin }, yenidenBasla) {
  const k = kok(); const bg = karartma();
  bg.style.background = "rgba(20,4,4,0.94)"; k.appendChild(bg);
  const fmt = (n) => n.toLocaleString("tr-TR") + " ₺";
  const kutu = document.createElement("div");
  Object.assign(kutu.style, { position: "absolute", left: "50%", top: "50%",
    transform: "translate(-50%,-50%)", width: "820px", maxHeight: "88%", overflow: "auto",
    padding: "40px 54px", textAlign: "center", borderRadius: "20px", pointerEvents: "auto",
    background: "#1a0a0a", border: "3px solid #7f1d1d", color: "#eee", lineHeight: "1.55" });
  kutu.innerHTML = `
    <div style="font-size:34px;font-weight:900;color:#ef4444;margin-bottom:20px">SENARYO TAMAMLANMIŞTIR</div>
    <div style="font-size:22px;text-align:left;margin:0 auto 22px;max-width:640px">
      Toplam yatırım: <b>${fmt(toplamYatirim)}</b><br>
      Son bakiye: <b>${fmt(sonBakiye)}</b><br>
      Net kayıp: <span style="color:#ef4444"><b>${fmt(netKayip)}</b></span><br>
      Toplam spin: ${toplamSpin}</div>
    <div style="font-size:22px;color:#ddd;margin-bottom:14px">
      Bu rakam, ortalama bir aile için yaklaşık
      <span style="color:#ef4444">2,5 aylık geçim giderine</span> karşılık gelir.</div>
    <div style="font-size:20px;color:#ccc;margin-bottom:22px">
      Unutulmamalıdır ki <span style="color:#16a34a"><b>sanal kumar bağımlılığı çözümsüz
      değildir</b></span> ve her zaman yeniden başlama imkânı vardır. Durumu
      <span style="color:#ea580c">ailenizle, amirlerinizle ve güvendiğiniz kişilerle</span>
      açıkça paylaşmak, çözüm yolunda atılacak <span style="color:#16a34a"><b>önemli bir
      adımdır</b></span>. <span style="color:#dc2626"><b>Yardım istemek bir zayıflık
      değil</b></span>; farkındalık ve değişim isteğinin göstergesidir.<br><br>
      <span style="color:#16a34a"><b>Yeşilay Danışmanlık Hattı: ${YESILAY}</b></span></div>`;
  const btn = document.createElement("button");
  btn.textContent = "YENİDEN BAŞLA";
  Object.assign(btn.style, { fontSize: "24px", fontWeight: "800", padding: "14px 46px",
    border: "0", borderRadius: "12px", cursor: "pointer", color: "#fff",
    background: "linear-gradient(180deg,#16a34a,#166534)" });
  btn.onclick = () => { bg.remove(); yenidenBasla(); };
  kutu.appendChild(btn); k.appendChild(kutu);
}
