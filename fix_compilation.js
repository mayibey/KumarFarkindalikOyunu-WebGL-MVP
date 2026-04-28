const fs = require('fs');
const strip = s => s.replace(/\r/g, '');

// ===== Fix UI.cs =====
{
    const raw = fs.readFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.UI.cs', 'utf8');
    const lines = raw.split('\n').map(strip);

    // FIX 1: Remove "?? tumbleSfxSource;" fragment injected between ShowParaCekPanel sig and its {
    // Lines 65-66 (0-indexed: 64-65): sig is at idx 64, fragment at 65, blank at 66, { at 67
    // Find the fragment line
    let fragIdx = -1;
    for (let i = 60; i < 75; i++) {
        if (lines[i].trim() === '?? tumbleSfxSource;') { fragIdx = i; break; }
    }
    if (fragIdx < 0) console.log('FIX1: fragment not found, skipping');
    else {
        // Remove fragment line and the blank line after it
        let toRemove = [fragIdx];
        if (lines[fragIdx + 1].trim() === '') toRemove.push(fragIdx + 1);
        for (let i = toRemove.length - 1; i >= 0; i--) lines.splice(toRemove[i], 1);
        console.log('FIX1: removed fragment at idx', fragIdx);
    }

    // FIX 2: Add missing final ?? line in bonusEndSfxSource assignment
    // Find "?? GameObject.Find("BonusSfxSource")?.GetComponent<AudioSource>()" not followed by ;
    let fixedFix2 = false;
    for (let i = 0; i < lines.length; i++) {
        const t = lines[i].trim();
        if (t === '?? GameObject.Find("BonusSfxSource")?.GetComponent<AudioSource>()') {
            // Check next non-blank line is NOT a ?? continuation
            const next = lines[i + 1] ? lines[i + 1].trim() : '';
            if (!next.startsWith('??') && !next.startsWith(';')) {
                lines.splice(i + 1, 0, '            ?? FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);');
                console.log('FIX2: inserted missing ?? line after idx', i);
                fixedFix2 = true;
                break;
            }
        }
    }
    if (!fixedFix2) console.log('FIX2: already fixed or not found');

    // FIX 3: Reconstruct OtomatikSpinKalanTextGuncelle - it's missing opening { and outer if wrapper
    // Find the broken sig
    let sigIdx = -1;
    for (let i = 0; i < lines.length; i++) {
        if (lines[i].trim() === 'private void OtomatikSpinKalanTextGuncelle()') { sigIdx = i; break; }
    }
    if (sigIdx < 0) {
        console.log('FIX3: OtomatikSpinKalanTextGuncelle sig not found');
    } else {
        // Find the end of the broken method block - look for the next method sig after sigIdx
        // The broken block is sigIdx to sigIdx+6 (7 lines: sig, if, blank, {, text, setactive, })
        // Replace with complete correct method
        const fullMethod = [
            '    private void OtomatikSpinKalanTextGuncelle()',
            '    {',
            '        if (otomatikSpinKalanText != null)',
            '        {',
            '            if (_otomatikSpinKalan > 0 && !bonusAktif)',
            '            {',
            '                otomatikSpinKalanText.text = $"Kalan Spin: {_otomatikSpinKalan}";',
            '                otomatikSpinKalanText.gameObject.SetActive(true);',
            '            }',
            '            else',
            '                otomatikSpinKalanText.gameObject.SetActive(false);',
            '        }',
            '        if (otomatikSpinButton != null)',
            '        {',
            '            var tmp = otomatikSpinButton.GetComponentInChildren<TMP_Text>(true);',
            '            if (tmp != null)',
            '                tmp.text = _otomatikSpinKalan > 0 ? "DURDUR" : (string.IsNullOrEmpty(otomatikSpinButtonNormalText) ? "Otomatik Spin" : otomatikSpinButtonNormalText);',
            '        }',
            '    }',
        ];

        // Find end of broken block: the next private/public method after sigIdx
        let endIdx = sigIdx;
        // The broken block has: sig, [blank?], if line, blank, {, body lines, }
        // Scan forward until we hit the next method sig or class end
        let braceDepth = 0;
        let foundBrace = false;
        for (let i = sigIdx + 1; i < Math.min(lines.length, sigIdx + 15); i++) {
            const t = lines[i].trim();
            for (const ch of t) {
                if (ch === '{') { braceDepth++; foundBrace = true; }
                else if (ch === '}') { braceDepth--; }
            }
            if (foundBrace && braceDepth === 0) { endIdx = i; break; }
            // If no brace yet and we hit another method sig, the block is just sig+stray lines
            if (!foundBrace && i > sigIdx + 1 && (t.startsWith('private ') || t.startsWith('public ') || t.startsWith('void ') || t.startsWith('IEnumerator '))) {
                endIdx = i - 1;
                // remove trailing blanks
                while (endIdx > sigIdx && lines[endIdx].trim() === '') endIdx--;
                break;
            }
        }
        if (endIdx === sigIdx) {
            // No brace found - just the sig with some lines, find next method
            for (let i = sigIdx + 1; i < lines.length; i++) {
                const t = lines[i].trim();
                if (t.startsWith('private void ') || t.startsWith('public void ') || t.startsWith('private IEnumerator ')) {
                    endIdx = i - 1;
                    while (endIdx > sigIdx && lines[endIdx].trim() === '') endIdx--;
                    break;
                }
            }
        }
        const removeCount = endIdx - sigIdx + 1;
        lines.splice(sigIdx, removeCount, ...fullMethod);
        console.log('FIX3: replaced broken OtomatikSpinKalanTextGuncelle (removed', removeCount, 'lines, inserted', fullMethod.length, ')');
    }

    // FIX 4: ShowBonusStartMessage has no body - it's followed directly by ShowBonusEndMessage sig
    let startMsgIdx = -1;
    for (let i = 0; i < lines.length; i++) {
        if (lines[i].trim() === 'private IEnumerator ShowBonusStartMessage()') { startMsgIdx = i; break; }
    }
    if (startMsgIdx < 0) {
        console.log('FIX4: ShowBonusStartMessage sig not found');
    } else {
        const nextLine = lines[startMsgIdx + 1] ? lines[startMsgIdx + 1].trim() : '';
        if (nextLine.startsWith('private IEnumerator ShowBonusEndMessage')) {
            // Insert body between the two sigs
            lines.splice(startMsgIdx + 1, 0,
                '    {',
                '        yield return StartCoroutine(_bonusUIServisi.ShowBonusStartMessage());',
                '    }',
                ''
            );
            console.log('FIX4: inserted ShowBonusStartMessage body at idx', startMsgIdx + 1);
        } else {
            console.log('FIX4: ShowBonusStartMessage already has a body, skipping');
        }
    }

    fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.UI.cs', lines.join('\n'), 'utf8');
    console.log('UI.cs written:', lines.length, 'lines');

    // Brace balance check
    let depth = 0;
    for (const l of lines) {
        const ci = l.indexOf('//');
        const check = ci >= 0 ? l.substring(0, ci) : l;
        for (const ch of check) {
            if (ch === '{') depth++;
            else if (ch === '}') depth--;
        }
    }
    console.log('UI.cs brace depth:', depth, depth === 0 ? 'OK' : 'ERROR');
}

// ===== Fix Main.cs =====
{
    const raw = fs.readFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', 'utf8');
    const lines = raw.split('\n').map(strip);

    // FIX A: Remove orphaned OtomatikSpinKalanTextGuncelle body (lines ~2157-2169)
    // Look for a { that starts a block containing "else\n    otomatikSpinKalanText.gameObject.SetActive(false)"
    // preceded directly by OtomatikSpinDurdur method close
    let fixAStart = -1;
    for (let i = 2100; i < Math.min(lines.length, 2200); i++) {
        const t = lines[i].trim();
        // A stray { at method-level indentation (4 spaces) with no preceding sig
        if (t === '{') {
            const prev = lines[i - 1] ? lines[i - 1].trim() : '';
            // Previous line should be closing brace of OtomatikSpinDurdur
            if (prev === '}') {
                // Check if content mentions otomatikSpinKalanText and otomatikSpinButton
                let lookAhead = lines.slice(i, i + 15).join(' ');
                if (lookAhead.includes('otomatikSpinKalanText') && lookAhead.includes('otomatikSpinButton')) {
                    fixAStart = i;
                    break;
                }
            }
        }
    }
    if (fixAStart < 0) {
        console.log('FIXA: orphaned OtomatikSpinKalanTextGuncelle body not found');
    } else {
        // Find end of this orphaned block
        let depth = 0, endIdx = fixAStart;
        for (let i = fixAStart; i < fixAStart + 20; i++) {
            for (const ch of lines[i]) {
                if (ch === '{') depth++;
                else if (ch === '}') depth--;
            }
            if (depth === 0) { endIdx = i; break; }
        }
        lines.splice(fixAStart, endIdx - fixAStart + 1);
        console.log('FIXA: removed orphaned body at', fixAStart, '-', endIdx, '(', endIdx - fixAStart + 1, 'lines)');
    }

    // FIX B: Remove orphaned ShowBonusStartMessage body (lines ~2301-2303)
    // Look for: { \n    yield return StartCoroutine(_bonusUIServisi.ShowBonusStartMessage()); \n }
    // preceded by BonusBaslangicAkisi method close
    let fixBIdx = -1;
    for (let i = 2200; i < Math.min(lines.length, 2400); i++) {
        if (lines[i].trim() === '{') {
            const body = lines[i + 1] ? lines[i + 1].trim() : '';
            const close = lines[i + 2] ? lines[i + 2].trim() : '';
            if (body.includes('_bonusUIServisi.ShowBonusStartMessage()') && close === '}') {
                fixBIdx = i;
                break;
            }
        }
    }
    if (fixBIdx < 0) {
        console.log('FIXB: orphaned ShowBonusStartMessage body not found');
    } else {
        lines.splice(fixBIdx, 3);
        console.log('FIXB: removed orphaned ShowBonusStartMessage body at', fixBIdx);
    }

    fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', lines.join('\n'), 'utf8');
    console.log('Main.cs written:', lines.length, 'lines');

    // Brace balance check
    let depth = 0;
    for (const l of lines) {
        const ci = l.indexOf('//');
        const check = ci >= 0 ? l.substring(0, ci) : l;
        for (const ch of check) {
            if (ch === '{') depth++;
            else if (ch === '}') depth--;
        }
    }
    console.log('Main.cs brace depth:', depth, depth === 0 ? 'OK' : 'ERROR');
}
