// Kayıt sistemi — SaveLoadServisi karşılığı. localStorage, Unity anahtar ŞEMASI korunur.
import { KAYIT } from "../../veri/sabitler.js";

export function kayitVar() { return !!localStorage.getItem(KAYIT.anaAnahtar); }

export function kaydet(veri) {
  try {
    localStorage.setItem(KAYIT.anaAnahtar, JSON.stringify({
      saveSurumu: KAYIT.saveSurumu,
      kullaniciAdi: veri.kullaniciAdi ?? "Misafir",
      aktifAsama: veri.asama ?? 0,
      aktifSpin: veri.spin ?? 1,
      toplamSpin: veri.toplamSpin ?? 0,
      sonBakiye: veri.bakiye ?? 50000,
      borcAlindi: veri.borcAlindi ?? false,
      toplamYatirim: veri.toplamYatirim ?? 0,
    }));
  } catch (e) { console.warn("[kayit] yazılamadı", e); }
}

export function yukle() {
  try { return JSON.parse(localStorage.getItem(KAYIT.anaAnahtar) || "null"); }
  catch { return null; }
}

export function sil() { localStorage.removeItem(KAYIT.anaAnahtar); }
