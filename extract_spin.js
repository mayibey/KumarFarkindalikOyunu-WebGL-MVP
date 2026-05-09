const fs = require('fs');
const raw = fs.readFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', 'utf8');
const lines = raw.split('\n');
const N = lines.length;
const strip = s => s.replace(/\r/g, '');

// Arrow-only methods (no brace body)
function isArrow(idx) {
    const l = strip(lines[idx]).trim();
    return l.includes('=>') && !l.startsWith('//') && !l.startsWith('*');
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

// Methods to extract (by exact trimmed signature)
const SPIN_SIGS = [
    'private IEnumerator PrecomputeNextSpinCoroutine(int odenebilirLimit, bool bonusSpin)',
    'private void ApplyNewGridAndSync(int[,] newGrid, int[,] newCarpanGrid)',
    'private void OncedenHesaplananSpinOnbelleginiTemizle()',
    'private bool SpinKaydiHamPaytableIleUyumluMu(SpinSimulasyonKaydi kayit, int bahis, int tumbleEsik)',
    'private IEnumerator IlkSpinPrecomputeGecikmeli()',
    'public void SpinButon()',
    'private void SpinButonImpl()',
    'private IEnumerator BirSpinHazirlaVeAt()',
    'private IEnumerator OtomatikSpinDongusu()',
    'public void OtomatikSpinDurdur()',
    'private void TryResumeOtomatikSpin()',
    'private SenaryoSpinPolitikasiBaglami OlusturSpinPolitikasiBaglami(bool ustUsteAktif)',
    'private SpinSimulasyonKaydi SimuleEtVeKaydetImpl(int odenebilirLimit, bool bonusSpin)',
];

function findSigLine(sigTrimmed) {
    for (let i = 0; i < N; i++) {
        const l = strip(lines[i]);
        const lt = l.trim();
        if (lt === sigTrimmed) {
            if ((l.startsWith('    ') && !l.startsWith('        ')) || !l.startsWith(' '))
                return i;
        }
    }
    // Looser: startsWith key part
    const key = sigTrimmed.split('(')[0];
    for (let i = 0; i < N; i++) {
        const l = strip(lines[i]);
        if (l.trim().startsWith(key) && !l.startsWith('        '))
            return i;
    }
    return -1;
}

const found = [];
const notFound = [];
for (const sig of SPIN_SIGS) {
    const sigLine = findSigLine(sig.trim());
    if (sigLine < 0) { notFound.push(sig); continue; }

    // Collect preceding comments/attributes
    let blockStart = sigLine;
    for (let k = sigLine - 1; k >= Math.max(0, sigLine - 6); k--) {
        const lt = strip(lines[k]).trim();
        if (lt === '' || lt.startsWith('///') || lt.startsWith('//') || lt.startsWith('[') || lt.startsWith('*'))
            blockStart = k;
        else break;
    }

    const endLine = findMethodEnd(sigLine);
    found.push({ sig: sig.trim(), blockStart, sigLine, endLine });
}

if (notFound.length > 0) {
    console.log('NOT FOUND:', notFound);
    process.exit(1);
}

// Check overlaps
let hasOverlap = false;
for (let i = 0; i < found.length; i++) {
    for (let j = i + 1; j < found.length; j++) {
        const a = found[i], b = found[j];
        if (a.sigLine < b.endLine && b.sigLine < a.endLine) {
            console.log('OVERLAP:', a.sig.substring(0, 40), '(' + (a.sigLine+1) + '-' + (a.endLine+1) + ') vs', b.sig.substring(0, 40), '(' + (b.sigLine+1) + '-' + (b.endLine+1) + ')');
            hasOverlap = true;
        }
    }
}
if (hasOverlap) { console.log('Fix overlaps first'); process.exit(1); }

console.log('All', found.length, 'methods found, no overlaps.');
for (const m of found) console.log('  ' + m.sig.substring(0, 60) + ' L' + (m.sigLine+1) + '-' + (m.endLine+1));

// ── Extra block: property block after SpinButon() end (SpinCalisiyorMu etc.)
// and constants block before OlusturSpinPolitikasiBaglami
const extras = [];

// Property block: SpinCalisiyorMu, BonusAktifMi, BotIcinBakiye, BotIcinBahis
// These are between SpinButon end and SpinButonImpl start
{
    const spinButonEntry = found.find(m => m.sig === 'public void SpinButon()');
    const spinImplEntry  = found.find(m => m.sig === 'private void SpinButonImpl()');
    if (spinButonEntry && spinImplEntry) {
        const propStart = spinButonEntry.endLine + 1;
        const propEnd   = spinImplEntry.blockStart - 1;
        if (propEnd >= propStart) {
            extras.push({ label: 'PropertiesBlock', blockStart: propStart, sigLine: propStart, endLine: propEnd });
            console.log('Extra (properties block): L' + (propStart+1) + '-' + (propEnd+1));
        }
    }
}

// Constants block: SIMULASYON_MAX_REROLL etc. + surrounding comments before OlusturSpinPolitikasiBaglami
{
    const oluEntry = found.find(m => m.sig.startsWith('private SenaryoSpinPolitikasiBaglami'));
    if (oluEntry) {
        // Walk back from blockStart to find const declarations
        let constsStart = oluEntry.blockStart;
        for (let k = oluEntry.blockStart - 1; k >= Math.max(0, oluEntry.blockStart - 8); k--) {
            const lt = strip(lines[k]).trim();
            if (lt === '' || lt.startsWith('///') || lt.startsWith('//') || lt.startsWith('private const') || lt.startsWith('['))
                constsStart = k;
            else break;
        }
        if (constsStart < oluEntry.blockStart) {
            extras.push({ label: 'ConstsBlock', blockStart: constsStart, sigLine: constsStart, endLine: oluEntry.blockStart - 1 });
            console.log('Extra (consts block): L' + (constsStart+1) + '-' + (oluEntry.blockStart) + ')');
        }
    }
}

// Merge extras into found for extraction
const allBlocks = [...found, ...extras];

// ── Build Spin.cs content ──
const spinHeader = [
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
const spinFooter = ['}'];

const spinLines = [];
// Sort by sigLine ascending for Spin.cs
const sortedAsc = [...allBlocks].sort((a, b) => a.sigLine - b.sigLine);
for (const m of sortedAsc) {
    if (spinLines.length > 0) spinLines.push('');
    for (let i = m.blockStart; i <= m.endLine; i++)
        spinLines.push(strip(lines[i]));
}

const spinContent = [...spinHeader, ...spinLines, ...spinFooter].join('\n');
fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.Spin.cs', spinContent, 'utf8');
console.log('Spin.cs written:', spinContent.split('\n').length, 'lines');

// ── Remove from main ──
// Sort all blocks by blockStart descending
const sortedDesc = [...allBlocks].sort((a, b) => b.blockStart - a.blockStart);
let mainLines = [...lines];
for (const m of sortedDesc) {
    mainLines.splice(m.blockStart, m.endLine - m.blockStart + 1);
}

// Collapse >2 consecutive blank lines
const cleaned = [];
let blankCount = 0;
for (const l of mainLines) {
    if (strip(l).trim() === '') { blankCount++; if (blankCount <= 2) cleaned.push(l); }
    else { blankCount = 0; cleaned.push(l); }
}

fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', cleaned.join('\n'), 'utf8');
console.log('Main.cs written:', cleaned.length, 'lines');

// ── Brace balance ──
function braceDepth(ls) {
    let d = 0;
    for (const l of ls) {
        const s = strip(l);
        const ci = s.indexOf('//');
        const check = ci >= 0 ? s.substring(0, ci) : s;
        for (const ch of check) {
            if (ch === '{') d++;
            else if (ch === '}') d--;
        }
    }
    return d;
}
console.log('Spin.cs brace depth:', braceDepth(spinContent.split('\n')), '(OK = 0)');
console.log('Main.cs brace depth:', braceDepth(cleaned), '(OK = 0)');
