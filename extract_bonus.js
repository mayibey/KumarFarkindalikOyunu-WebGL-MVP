const fs = require('fs');
const raw = fs.readFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', 'utf8');
const lines = raw.split('\n');
const N = lines.length;
const strip = s => s.replace(/\r/g, '');

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

const BONUS_SIGS = [
    'private void BaslatBonus()',
    'private IEnumerator BonusBaslangicAkisi()',
    'private int GetBonusRemainingPayableTL()',
    'private void InitBonusBudgetFromHavuz(long odulHavuzuTL)',
    'private void RecordBonusPayment(int odenenTL)',
    'private IEnumerator CollapseRefillAndAnimate()',
];

function findSigLine(sigTrimmed) {
    for (let i = 0; i < N; i++) {
        const l = strip(lines[i]);
        if (l.trim() === sigTrimmed) {
            if ((l.startsWith('    ') && !l.startsWith('        ')) || !l.startsWith(' '))
                return i;
        }
    }
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
for (const sig of BONUS_SIGS) {
    const sigLine = findSigLine(sig.trim());
    if (sigLine < 0) { notFound.push(sig); continue; }

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

if (notFound.length > 0) { console.log('NOT FOUND:', notFound); process.exit(1); }

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
if (hasOverlap) { console.log('Fix overlaps'); process.exit(1); }

console.log('All', found.length, 'methods found, no overlaps.');
for (const m of found) console.log('  ' + m.sig.substring(0, 60) + ' L' + (m.sigLine+1) + '-' + (m.endLine+1));

// ── Build Bonus.cs ──
const header = [
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
const footer = ['}'];

const bonusLines = [];
const sortedAsc = [...found].sort((a, b) => a.sigLine - b.sigLine);
for (const m of sortedAsc) {
    if (bonusLines.length > 0) bonusLines.push('');
    for (let i = m.blockStart; i <= m.endLine; i++)
        bonusLines.push(strip(lines[i]));
}

const bonusContent = [...header, ...bonusLines, ...footer].join('\n');
fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.Bonus.cs', bonusContent, 'utf8');
console.log('Bonus.cs written:', bonusContent.split('\n').length, 'lines');

// ── Remove from main ──
const sortedDesc = [...found].sort((a, b) => b.blockStart - a.blockStart);
let mainLines = [...lines];
for (const m of sortedDesc)
    mainLines.splice(m.blockStart, m.endLine - m.blockStart + 1);

// Collapse >2 consecutive blanks
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
console.log('Bonus.cs brace depth:', braceDepth(bonusContent.split('\n')), '(OK=0)');
console.log('Main.cs brace depth:', braceDepth(cleaned), '(OK=0)');
