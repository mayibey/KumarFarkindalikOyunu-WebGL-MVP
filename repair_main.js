const fs = require('fs');
const raw = fs.readFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', 'utf8');
const lines = raw.split('\n');
const strip = s => s.replace(/\r/g,'');

const r9 = [
    '    private void BaslatGeciciGlobalTiklamaKilidi(float sure)',
    '    {',
    '        if (sure <= 0f) return;',
    '        if (_geciciTiklamaKilidiCoroutine != null)',
    '            StopCoroutine(_geciciTiklamaKilidiCoroutine);',
    '        _geciciTiklamaKilidiCoroutine = StartCoroutine(GeciciGlobalTiklamaKilidiCoroutine(sure));',
    '    }',
    '',
    '    private IEnumerator GeciciGlobalTiklamaKilidiCoroutine(float sure)',
    '    {',
    '        EnsureGlobalTiklamaKilidiPanel();',
    '        _geciciTiklamaKilidiAktif = true;',
    '        UygulaGlobalTiklamaKilidiGorunurlugu();',
    '        yield return new WaitForSecondsRealtime(sure);',
    '        _geciciTiklamaKilidiAktif = false;',
    '        UygulaGlobalTiklamaKilidiGorunurlugu();',
    '        _geciciTiklamaKilidiCoroutine = null;',
    '    }',
    '',
    '    private void EnsureGlobalTiklamaKilidiPanel()',
    '    {',
    '        if (_geciciTiklamaKilidiPanel != null) return;',
    '        _geciciTiklamaKilidiPanel = new GameObject("GeciciTiklamaKilidiPanel");',
    '        var rt = _geciciTiklamaKilidiPanel.AddComponent<RectTransform>();',
    '        var canvas = _geciciTiklamaKilidiPanel.AddComponent<Canvas>();',
    '        canvas.renderMode = RenderMode.ScreenSpaceOverlay;',
    '        canvas.overrideSorting = true;',
    '        canvas.sortingOrder = 32760;',
    '        _geciciTiklamaKilidiPanel.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;',
    '        _geciciTiklamaKilidiPanel.AddComponent<GraphicRaycaster>();',
    '        var img = _geciciTiklamaKilidiPanel.AddComponent<Image>();',
    '        img.color = new Color(0f, 0f, 0f, 0f);',
    '        img.raycastTarget = true;',
    '        rt.anchorMin = Vector2.zero;',
    '        rt.anchorMax = Vector2.one;',
    '        rt.offsetMin = Vector2.zero;',
    '        rt.offsetMax = Vector2.zero;',
    '        DontDestroyOnLoad(_geciciTiklamaKilidiPanel);',
    '    }',
    '',
    '    private void SetGlobalTiklamaKilidi(bool aktif)',
    '    {',
    '        _manuelGlobalTiklamaKilidiAktif = aktif;',
    '        UygulaGlobalTiklamaKilidiGorunurlugu();',
    '    }',
    '',
    '    private void UygulaGlobalTiklamaKilidiGorunurlugu()',
    '    {',
    '        EnsureGlobalTiklamaKilidiPanel();',
    '        bool aktif = false;',
    '        if (_geciciTiklamaKilidiPanel != null)',
    '            _geciciTiklamaKilidiPanel.SetActive(aktif);',
    '    }',
];

const r8 = [
    '    public void BahisArttir()',
    '    {',
    '        if (_ekonomiServisi == null) return;',
    '        _ekonomiServisi.BahisArttir();',
    '        SenaryoYoneticisi.I?.UI_Guncelle();',
    '    }',
    '    public void BahisAzalt()',
    '    {',
    '        if (_ekonomiServisi == null) return;',
    '        _ekonomiServisi.BahisAzalt();',
    '        SenaryoYoneticisi.I?.UI_Guncelle();',
    '    }',
];

const r7 = [
    '',
    '    public void HideBakiyeYuklePanel()',
    '    {',
    '        if (bakiyeYuklePanel != null)',
    '            bakiyeYuklePanel.SetActive(false);',
    '    }',
    '',
    '    public void BonusSatinAl()',
    '    {',
    '        if (_bonusAyarlari == null)',
    '            _bonusAyarlari = FindFirstObjectByType<BonusAyarlari>(FindObjectsInactive.Include);',
    '        Debug.Log("[BONUS SATIN AL] Buton tiklandi.");',
    '        _bonusUIServisi?.BonusSatinAlRequested();',
    '    }',
    '',
    '    public void BonusSatinAlOnayla() => _bonusUIServisi?.OnYes();',
    '    public void BonusSatinAlIptal() => _bonusUIServisi?.OnNo();',
    '',
    '    private void ShowBonusBuyConfirmPanel(int cost)',
    '    {',
    '        _bonusUIServisi?.ShowBonusBuyConfirmPanel(cost);',
    '        _uiServisi?.UI_Guncelle();',
    '    }',
    '',
    '    private void HideBonusBuyConfirmPanel()',
    '    {',
    '        _bonusUIServisi?.HideBonusBuyConfirmPanel();',
    '        _uiServisi?.UI_Guncelle();',
    '    }',
    '',
    '    private void OnBonusBuyYes() => _bonusUIServisi?.OnYes();',
    '    private void OnBonusBuyNo() => _bonusUIServisi?.OnNo();',
    '',
    '    void BonusMiktariYazisiniGuncelle(int maliyet, GameObject panel)',
    '    {',
    '        long bonusMiktari = Mathf.Max(0, maliyet);',
    '        string formatliMiktar = OyunFormatServisi',
    '            .FormatTL((int)Mathf.Min(int.MaxValue, bonusMiktari))',
    '            .Replace(" TL", "\u00A0TL");',
    '        string metin = formatliMiktar + " karsiligindan bonus oyun almak istiyor musunuz?";',
    '',
    '        if (bonusBuyConfirmCostText != null)',
    '        {',
    '            bonusBuyConfirmCostText.text = metin;',
    '            return;',
    '        }',
    '',
    '        if (panel == null) return;',
    '        var tumMetinler = panel.GetComponentsInChildren<TMP_Text>(true);',
    '        foreach (var tmp in tumMetinler)',
    '        {',
    '            if (tmp == null || tmp.gameObject == null) continue;',
    '            string ad = tmp.gameObject.name.ToLowerInvariant();',
    '            if (ad.Contains("maliyet") || ad.Contains("cost") || ad.Contains("bonusmiktar"))',
    '            {',
    '                tmp.text = metin;',
    '                return;',
    '            }',
    '        }',
    '    }',
];

const r6 = [
    '    }',
    '',
    '    public void ShowBakiyeYuklePanel(bool yetersizBakiyeUyarisi = false)',
    '    {',
];

const animPart = lines.slice(2365, 2397).map(strip);
const camUsage = lines.slice(2397, 2401).map(strip);
const animLoop = lines.slice(2404, 2426).map(strip);
const kazancComment = strip(lines[2401]);
const kazancBody = lines.slice(2429, 2484).map(strip);

const r5 = [
    ...animPart,
    '        Camera cam = null;',
    '        Canvas bakiyeCanvas = bakiyeText.canvas;',
    ...camUsage,
    '',
    ...animLoop,
    '',
    kazancComment,
    '    private IEnumerator KazancKutusunaCarpanVurusPlusAnimasyonu(int carpanDeger)',
    ...kazancBody,
];

console.log('R5=' + r5.length + '(was ' + (2483-2365+1) + ') R6=' + r6.length + '(was 5) R7=' + r7.length + '(was ' + (2596-2540+1) + ') R8=' + r8.length + '(was 13) R9=' + r9.length + '(was ' + (2752-2699+1) + ')');

let newLines = [...lines];
newLines.splice(2699, 2752-2699+1, ...r9);
newLines.splice(2597, 2609-2597+1, ...r8);
newLines.splice(2540, 2596-2540+1, ...r7);
newLines.splice(2516, 2520-2516+1, ...r6);
newLines.splice(2365, 2483-2365+1, ...r5);

fs.writeFileSync('D:/KumarFarkindalikOyunu/Assets/Scripts/OyunYoneticisi.cs', newLines.join('\n'), 'utf8');
console.log('Written. Total lines: ' + newLines.length);
