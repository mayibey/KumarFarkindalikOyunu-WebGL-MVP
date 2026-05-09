const fs = require('fs');
const raw = fs.readFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', 'utf8');
const lines = raw.split('\n');
const N = lines.length;
const strip = s => s.replace(/\r/g,'');

const UI_SIGS = [
  'private void CloseMoneyPanels()',
  'public void ParaCek_OnayButton()',
  'public void ParaCek_IptalButton()',
  'public void BakiyeYukle_OnayButton()',
  'private IEnumerator BakiyeYukleButonKilidiCoroutine(float sure)',
  'public void BakiyeYukle_IptalButton()',
  'public void ShowParaCekPanel()',
  'public void HideParaCekPanel()',
  'private void InspectorBakiyesiniYansit()',
  'private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)',
  'private static void AddBtnLabel(GameObject btnGo, string label, float fontSize)',
  'private void SenaryoDropdownYazilariniBuyut()',
  'private void BahisGorselKilidiniHazirla()',
  'private void BahisGorselKilidiniUygula()',
  'public void InspectorBakiyesiniSimdiUygula()',
  'private void EnsureNormalSpinSonucPopup()',
  'private IEnumerator ShowNormalSpinSonucPopup(int odenen, int bahis)',
  'private IEnumerator AnimateNormalSpinSonucBakiyeAkisi(int net)',
  'private IEnumerator KazancKutusunaCarpanVurusPlusAnimasyonu(int carpanDeger)',
  'private void PlayNormalSpinSonucSesi(int odenen, int bahis)',
  'private void SesKaynaklariniHazirla()',
  'public void ShowBakiyeYuklePanel(bool yetersizBakiyeUyarisi = false)',
  'public void HideBakiyeYuklePanel()',
  'public void BonusSatinAl()',
  'public void BonusSatinAlOnayla() => _bonusUIServisi?.OnYes();',
  'public void BonusSatinAlIptal() => _bonusUIServisi?.OnNo();',
  'private void ShowBonusBuyConfirmPanel(int cost)',
  'private void HideBonusBuyConfirmPanel()',
  'private void OnBonusBuyYes() => _bonusUIServisi?.OnYes();',
  'private void OnBonusBuyNo() => _bonusUIServisi?.OnNo();',
  'void BonusMiktariYazisiniGuncelle(int maliyet, GameObject panel)',
  'public void BahisArttir()',
  'public void BahisAzalt()',
  'private void BaslatGeciciGlobalTiklamaKilidi(float sure)',
  'private IEnumerator GeciciGlobalTiklamaKilidiCoroutine(float sure)',
  'private void EnsureGlobalTiklamaKilidiPanel()',
  'private void SetGlobalTiklamaKilidi(bool aktif)',
  'private void UygulaGlobalTiklamaKilidiGorunurlugu()',
  'private void OtomatikSpinKalanTextGuncelle()',
  'private void OnOtomatikSpinDropdownChanged(int index)',
  'private void OnOtomatikSpinButtonClick()',
  'private void OnOtomatikSpinBaslatClick()',
  'private void OnOtomatikSpinIptalClick()',
  'private void IstatistikButonTiklandi()',
  'private void YoneticiButonTiklandi()',
  'private IEnumerator ShowBonusStartMessage()',
  'private void TrySpawnCarpanOverlay(int carpanDegeri)',
  'private void ClearAllCarpanOverlays()',
  'private void UI_CarpanSifirla()',
  'private void UI_CarpanGuncelle()',
  'private IEnumerator ScatterBuyutEfekti()',
];

function isArrow(idx) {
  const l = strip(lines[idx]).trim();
  return l.includes('=>') && !l.startsWith('//');
}

function findMethodEnd(startLine) {
  if (isArrow(startLine)) return startLine;
  let depth = 0, foundOpen = false;
  for (let i = startLine; i < N; i++) {
    const l = strip(lines[i]);
    const ci = l.indexOf('//');
    const check = ci >= 0 ? l.substring(0, ci) : l;
    for (const ch of check) {
      if (ch === '{') { depth++; foundOpen = true; }
      else if (ch === '}') { depth--; if (foundOpen && depth === 0) return i; }
    }
  }
  return startLine;
}

const found = [];
let notFound = [];
for (const sig of UI_SIGS) {
  const sigTrimmed = sig.trim();
  let sigLine = -1;
  for (let i = 0; i < N; i++) {
    const l = strip(lines[i]);
    const lt = l.trim();
    if (lt === sigTrimmed) {
      if ((l.startsWith('    ') && !l.startsWith('        ')) || !l.startsWith(' ')) {
        sigLine = i; break;
      }
    }
  }
  if (sigLine < 0) {
    // looser match
    const key = sigTrimmed.split('(')[0];
    for (let i = 0; i < N; i++) {
      const l = strip(lines[i]);
      if (l.trim().startsWith(key) && !l.startsWith('        ')) {
        sigLine = i; break;
      }
    }
  }
  if (sigLine < 0) { notFound.push(sig); continue; }

  let blockStart = sigLine;
  for (let k = sigLine - 1; k >= Math.max(0, sigLine - 6); k--) {
    const lt = strip(lines[k]).trim();
    if (lt === '' || lt.startsWith('///') || lt.startsWith('//') || lt.startsWith('[') || lt.startsWith('*')) {
      blockStart = k;
    } else break;
  }

  const endLine = findMethodEnd(sigLine);
  found.push({ sig: sigTrimmed, blockStart, sigLine, endLine });
}

if (notFound.length > 0) {
  console.log('NOT FOUND:', notFound);
  process.exit(1);
}

// Check for overlaps
let hasOverlap = false;
for (let i = 0; i < found.length; i++) {
  for (let j = i+1; j < found.length; j++) {
    const a = found[i], b = found[j];
    if (a.sigLine < b.endLine && b.sigLine < a.endLine) {
      console.log('OVERLAP: ' + a.sig.substring(0,40) + ' (' + (a.sigLine+1) + '-' + (a.endLine+1) + ') vs ' + b.sig.substring(0,40) + ' (' + (b.sigLine+1) + '-' + (b.endLine+1) + ')');
      hasOverlap = true;
    }
  }
}
if (hasOverlap) { console.log('Fix overlaps first'); process.exit(1); }

console.log('All', found.length, 'methods found, no overlaps. Proceeding with extraction...');

// Sort by blockStart descending (for removal from main)
const sortedDesc = [...found].sort((a,b) => b.blockStart - a.blockStart);

// Build UI.cs content
const uiHeader = [
  'using System;',
  'using System.Collections;',
  'using System.Collections.Generic;',
  'using UnityEngine;',
  'using UnityEngine.UI;',
  'using UnityEngine.SceneManagement;',
  'using TMPro;',
  '',
  'public partial class OyunYoneticisi',
  '{',
];
const uiFooter = ['}'];

const uiLines = [];
// Sort by sigLine ascending for UI.cs (original order)
const sortedAsc = [...found].sort((a,b) => a.sigLine - b.sigLine);
for (const m of sortedAsc) {
  if (uiLines.length > 0) uiLines.push('');
  for (let i = m.blockStart; i <= m.endLine; i++) {
    uiLines.push(strip(lines[i]));
  }
}

const uiContent = [...uiHeader, ...uiLines, ...uiFooter].join('\n');
fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.UI.cs', uiContent, 'utf8');
console.log('UI.cs written:', uiContent.split('\n').length, 'lines');

// Build cleaned main.cs (remove all UI method blocks)
let mainLines = [...lines];
for (const m of sortedDesc) {
  // Find actual block start (may include preceding blank line)
  let removeStart = m.blockStart;
  // If the line before blockStart is blank, include it only if not used by previous method
  mainLines.splice(removeStart, m.endLine - removeStart + 1);
}

// Remove consecutive blank lines (max 2)
const cleaned = [];
let blankCount = 0;
for (const l of mainLines) {
  if (strip(l).trim() === '') { blankCount++; if (blankCount <= 2) cleaned.push(l); }
  else { blankCount = 0; cleaned.push(l); }
}

fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', cleaned.join('\n'), 'utf8');
console.log('Main.cs written:', cleaned.length, 'lines');

// Verify brace balance of main
let depth2 = 0;
for (const l of cleaned) {
  const s = strip(l);
  const ci = s.indexOf('//');
  const check = ci >= 0 ? s.substring(0, ci) : s;
  for (const ch of check) {
    if (ch === '{') depth2++;
    else if (ch === '}') depth2--;
  }
}
console.log('Main brace depth:', depth2, depth2 === 0 ? 'OK' : 'ERROR');
