// Giriş ekranı — 01_GirisScene karşılığı: isim girişi + 2 buton
// (SENARYOLU OYUNA BAŞLA + MANİPÜLASYON PANELİNE GİT). Arka planda oyun demo döner.
// DOM overlay (#sahne içi, deck ölçekli). Kayıt varsa "DEVAM ET" seçeneği.
import { kayitVar, yukle, sil } from "../cekirdek/kayit.js";

export function girisEkraniGoster({ onSenaryo, onDevam, onPanel }) {
  return new Promise((coz) => {
    const kok = document.createElement("div");
    kok.id = "girisEkraniKok";
    Object.assign(kok.style, { position: "absolute", inset: "0",
      background: "rgba(6,8,14,0.90)", zIndex: "80", pointerEvents: "auto",
      display: "flex", flexDirection: "column", alignItems: "center",
      justifyContent: "center", gap: "26px",
      fontFamily: "'LilitaOne','Segoe UI',sans-serif" });
    document.getElementById("sahne").appendChild(kok);

    const baslik = document.createElement("div");
    baslik.innerHTML = `<span style="color:#f4d678">KUMAR</span>
      <span style="color:#e63946">KAZANDIRMAZ</span>`;
    Object.assign(baslik.style, { fontSize: "84px", fontWeight: "900",
      letterSpacing: "2px", textShadow: "0 4px 20px rgba(0,0,0,.7)" });
    kok.appendChild(baslik);

    const input = document.createElement("input");
    input.placeholder = "Adınızı girin"; input.maxLength = 20;
    Object.assign(input.style, { fontSize: "30px", padding: "14px 24px",
      width: "460px", textAlign: "center", borderRadius: "12px",
      border: "2px solid #d4a24a", background: "#1a1508", color: "#fff",
      fontFamily: "inherit" });
    kok.appendChild(input);

    const btnStil = (arka) => ({ fontSize: "30px", fontWeight: "800",
      padding: "18px 40px", width: "520px", border: "0", borderRadius: "14px",
      cursor: "pointer", color: "#1a1000", fontFamily: "inherit",
      background: arka, boxShadow: "0 8px 24px rgba(0,0,0,.5)" });

    const kaydetVarMi = kayitVar();
    if (kaydetVarMi) {
      const k = yukle();
      const devamBtn = document.createElement("button");
      devamBtn.textContent = `DEVAM ET (${k?.kullaniciAdi || "Misafir"} · Aşama ${(k?.aktifAsama ?? 0) + 1})`;
      Object.assign(devamBtn.style, btnStil("linear-gradient(180deg,#7dd3a0,#37b98a)"));
      devamBtn.onclick = () => { kok.remove(); onDevam(k); coz(); };
      kok.appendChild(devamBtn);
    }

    const senaryoBtn = document.createElement("button");
    senaryoBtn.textContent = "SENARYOLU OYUNA BAŞLA";
    Object.assign(senaryoBtn.style, btnStil("linear-gradient(180deg,#f4d678,#d8a63a)"));
    senaryoBtn.onclick = () => {
      const ad = (input.value || "").trim() || "Misafir";
      if (kaydetVarMi) sil();
      kok.remove(); onSenaryo(ad); coz();
    };
    kok.appendChild(senaryoBtn);

    const panelBtn = document.createElement("button");
    panelBtn.textContent = "MANİPÜLASYON PANELİNE GİT";
    Object.assign(panelBtn.style, btnStil("linear-gradient(180deg,#e8a05a,#c0632a)"));
    panelBtn.onclick = () => { kok.remove(); onPanel(); coz(); };
    kok.appendChild(panelBtn);
  });
}
