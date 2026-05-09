import sys

path = 'D:/KumarFarkindalikOyunu/Assets/Scenes/06_AdminOyunKopya.unity'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. WinFeedbackUI RectTransform children
old_rt = '  m_Children: []\n  m_Father: {fileID: 1345215518}'
new_rt = '  m_Children:\n  - {fileID: 9870011}\n  m_Father: {fileID: 1345215518}'
assert old_rt in content, 'RT anchor not found'
content = content.replace(old_rt, new_rt, 1)

# 2. WinFeedbackUI MonoBehaviour field bindings
old_mb = ('  panelCanvasGroup: {fileID: 0}\n'
          '  olcekKoku: {fileID: 9870002}\n'
          '  baslikText: {fileID: 0}\n'
          '  kazancText: {fileID: 0}\n')
new_mb = ('  panelCanvasGroup: {fileID: 9870013}\n'
          '  olcekKoku: {fileID: 9870011}\n'
          '  baslikText: {fileID: 9870022}\n'
          '  kazancText: {fileID: 9870032}\n')
assert old_mb in content, 'MB anchor not found'
content = content.replace(old_mb, new_mb, 1)

TMP  = 'f4688fdb7df04437aeb418b961361dc5'
IMG  = 'fe87c0e1cc204ed48ad3b37840f39efc'
FONT = '8f586378b4e144a9851e7b34d9b748ee'
MAT  = '2180264'
LIRA = '\u20ba'

def go_block(fid, comps, name):
    c = '\n'.join(f'  - component: {{fileID: {x}}}' for x in comps)
    return (f'--- !u!1 &{fid}\nGameObject:\n'
            f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            f'  serializedVersion: 6\n  m_Component:\n{c}\n'
            f'  m_Layer: 5\n  m_Name: {name}\n  m_TagString: Untagged\n'
            f'  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n'
            f'  m_StaticEditorFlags: 0\n  m_IsActive: 1\n')

def rt_block(fid, gofid, father, children, amin, amax):
    if children:
        ch = '  m_Children:\n' + '\n'.join(f'  - {{fileID: {x}}}' for x in children)
    else:
        ch = '  m_Children: []'
    return (f'--- !u!224 &{fid}\nRectTransform:\n'
            f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {gofid}}}\n'
            f'  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}\n'
            f'  m_LocalPosition: {{x: 0, y: 0, z: 0}}\n'
            f'  m_LocalScale: {{x: 1, y: 1, z: 1}}\n'
            f'  m_ConstrainProportionsScale: 0\n{ch}\n'
            f'  m_Father: {{fileID: {father}}}\n'
            f'  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'
            f'  m_AnchorMin: {{x: {amin[0]}, y: {amin[1]}}}\n'
            f'  m_AnchorMax: {{x: {amax[0]}, y: {amax[1]}}}\n'
            f'  m_AnchoredPosition: {{x: 0, y: 0}}\n'
            f'  m_SizeDelta: {{x: 0, y: 0}}\n  m_Pivot: {{x: 0.5, y: 0.5}}\n')

def img_block(fid, gofid):
    return (f'--- !u!114 &{fid}\nMonoBehaviour:\n'
            f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {gofid}}}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
            f'  m_Script: {{fileID: 11500000, guid: {IMG}, type: 3}}\n'
            f'  m_Name: \n'
            f'  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n'
            f'  m_Material: {{fileID: 0}}\n'
            f'  m_Color: {{r: 0, g: 0, b: 0, a: 0.78}}\n'
            f'  m_RaycastTarget: 0\n'
            f'  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n'
            f'  m_Maskable: 1\n'
            f'  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
            f'  m_Sprite: {{fileID: 0}}\n  m_Type: 0\n  m_PreserveAspect: 0\n'
            f'  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n'
            f'  m_FillClockwise: 1\n  m_FillOrigin: 0\n'
            f'  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: 1\n')

def cg_block(fid, gofid):
    return (f'--- !u!225 &{fid}\nCanvasGroup:\n'
            f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {gofid}}}\n'
            f'  m_Enabled: 1\n  m_Alpha: 0\n'
            f'  m_Interactable: 0\n  m_BlocksRaycasts: 0\n  m_IgnoreParentGroups: 0\n')

def cr_block(fid, gofid):
    return (f'--- !u!222 &{fid}\nCanvasRenderer:\n'
            f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {gofid}}}\n  m_CullTransparentMesh: 1\n')

def tmp_block(fid, gofid, text, fontsize, bold, rgba_val, fr, fg, fb):
    weight = 700 if bold else 400
    style  = 1   if bold else 0
    fmin   = 36  if bold else 24
    return (f'--- !u!114 &{fid}\nMonoBehaviour:\n'
            f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            f'  m_GameObject: {{fileID: {gofid}}}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
            f'  m_Script: {{fileID: 11500000, guid: {TMP}, type: 3}}\n'
            f'  m_Name: \n'
            f'  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI\n'
            f'  m_Material: {{fileID: 0}}\n  m_Color: {{r: 1, g: 1, b: 1, a: 1}}\n'
            f'  m_RaycastTarget: 0\n'
            f'  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n'
            f'  m_Maskable: 1\n'
            f'  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
            f'  m_text: {text}\n  m_isRightToLeft: 0\n'
            f'  m_fontAsset: {{fileID: 11400000, guid: {FONT}, type: 2}}\n'
            f'  m_sharedMaterial: {{fileID: {MAT}, guid: {FONT}, type: 2}}\n'
            f'  m_fontSharedMaterials: []\n  m_fontMaterial: {{fileID: 0}}\n'
            f'  m_fontMaterials: []\n'
            f'  m_fontColor32:\n    serializedVersion: 2\n    rgba: {rgba_val}\n'
            f'  m_fontColor: {{r: {fr}, g: {fg}, b: {fb}, a: 1}}\n'
            f'  m_enableVertexGradient: 0\n  m_colorMode: 3\n'
            f'  m_fontColorGradient:\n'
            f'    topLeft: {{r: 1, g: 1, b: 1, a: 1}}\n'
            f'    topRight: {{r: 1, g: 1, b: 1, a: 1}}\n'
            f'    bottomLeft: {{r: 1, g: 1, b: 1, a: 1}}\n'
            f'    bottomRight: {{r: 1, g: 1, b: 1, a: 1}}\n'
            f'  m_fontColorGradientPreset: {{fileID: 0}}\n'
            f'  m_spriteAsset: {{fileID: 0}}\n  m_tintAllSprites: 0\n'
            f'  m_StyleSheet: {{fileID: 0}}\n  m_TextStyleHashCode: -1183493901\n'
            f'  m_overrideHtmlColors: 0\n'
            f'  m_faceColor:\n    serializedVersion: 2\n    rgba: 4294967295\n'
            f'  m_fontSize: {fontsize}\n  m_fontSizeBase: {fontsize}\n'
            f'  m_fontWeight: {weight}\n'
            f'  m_enableAutoSizing: 1\n  m_fontSizeMin: {fmin}\n  m_fontSizeMax: {fontsize}\n'
            f'  m_fontStyle: {style}\n'
            f'  m_HorizontalAlignment: 2\n  m_VerticalAlignment: 512\n'
            f'  m_textAlignment: 65535\n'
            f'  m_characterSpacing: 0\n  m_characterHorizontalScale: 1\n'
            f'  m_wordSpacing: 0\n  m_lineSpacing: 0\n  m_lineSpacingMax: 0\n'
            f'  m_paragraphSpacing: 0\n  m_charWidthMaxAdj: 0\n'
            f'  m_TextWrappingMode: 0\n  m_wordWrappingRatios: 0.4\n'
            f'  m_overflowMode: 0\n'
            f'  m_linkedTextComponent: {{fileID: 0}}\n  parentLinkedComponent: {{fileID: 0}}\n'
            f'  m_enableKerning: 1\n  m_ActiveFontFeatures: 6e72656b\n'
            f'  m_enableExtraPadding: 0\n  checkPaddingRequired: 0\n'
            f'  m_isRichText: 1\n  m_EmojiFallbackSupport: 1\n  m_parseCtrlCharacters: 1\n'
            f'  m_isOrthographic: 1\n  m_isCullingEnabled: 0\n'
            f'  m_horizontalMapping: 0\n  m_verticalMapping: 0\n  m_uvLineOffset: 0\n'
            f'  m_geometrySortingOrder: 0\n  m_IsTextObjectScaleStatic: 0\n'
            f'  m_VertexBufferAutoSizeReduction: 0\n  m_useMaxVisibleDescender: 1\n'
            f'  m_pageToDisplay: 1\n  m_margin: {{x: 0, y: 0, z: 0, w: 0}}\n'
            f'  m_isUsingLegacyAnimationComponent: 0\n  m_isVolumetricText: 0\n'
            f'  m_hasFontAssetChanged: 0\n  m_baseMaterial: {{fileID: 0}}\n'
            f'  m_maskOffset: {{x: 0, y: 0, z: 0, w: 0}}\n')

new_blocks = (
    go_block(9870010, [9870011, 9870012, 9870013, 9870014], 'WinPanel') +
    rt_block(9870011, 9870010, 9870002, [9870021, 9870031], (0,0), (1,1)) +
    img_block(9870012, 9870010) +
    cg_block(9870013, 9870010) +
    cr_block(9870014, 9870010) +
    go_block(9870020, [9870021, 9870022, 9870023], 'BaslikText') +
    rt_block(9870021, 9870020, 9870011, [], (0.1, 0.55), (0.9, 0.85)) +
    tmp_block(9870022, 9870020, 'BIG WIN', 88, True, 4281589503, 1, 0.87058824, 0.2) +
    cr_block(9870023, 9870020) +
    go_block(9870030, [9870031, 9870032, 9870033], 'KazancText') +
    rt_block(9870031, 9870030, 9870011, [], (0.1, 0.3), (0.9, 0.55)) +
    tmp_block(9870032, 9870030, LIRA + '0', 56, True, 4294967295, 1, 1, 1) +
    cr_block(9870033, 9870030)
)

if not content.endswith('\n'):
    content += '\n'
content += new_blocks

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('OK - sahne guncellendi')
