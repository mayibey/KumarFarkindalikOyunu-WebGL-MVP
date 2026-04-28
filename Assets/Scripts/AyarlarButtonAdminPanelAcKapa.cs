using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <c>AyarlarButton</c> yönetici ayar panelini (<c>AdminSettingsPanel</c>) açar; <c>CloseButton</c> kapatır.
/// Bileşen <c>AyarlarButton</c> üzerinde olabilir veya Inspector'dan buton atanır / isimle bulunur.
/// </summary>
[DisallowMultipleComponent]
public class AyarlarButtonAdminPanelAcKapa : MonoBehaviour
{
    const string VarsayilanButonAdi = "AyarlarButton";
    const string KapatButonAdi = "CloseButton";

    [Tooltip("Boşsa bu objede veya sahnede \"AyarlarButton\" adlı nesnede Button aranır.")]
    [SerializeField] Button ayarlarButonu;

    [Tooltip("Boşsa sahnede AdminSettingsPanel (AdminSettingsPanelYanKaydirici) aranır.")]
    [SerializeField] GameObject adminAyarlarPaneli;

    [Tooltip("Yedek: panel üstünde YanKaydirici yoksa ve isimle bulunacaksa (yalnızca aktif nesnelerde çalışır).")]
    [SerializeField] string panelGameObjectAdi = "AdminSettingsPanel";

    Button _bagliAyarlarButon;
    Button _bagliKapatButon;

    void Awake()
    {
        AdminPaneliniCozumle();
        AyarlarButonunuBagla();
        KapatButonunuBagla();
    }

    void OnDestroy()
    {
        if (_bagliAyarlarButon != null)
            _bagliAyarlarButon.onClick.RemoveListener(AdminPaneliniAc);
        if (_bagliKapatButon != null)
            _bagliKapatButon.onClick.RemoveListener(AdminPaneliniKapat);
    }

    void AyarlarButonunuBagla()
    {
        _bagliAyarlarButon = ayarlarButonu;
        if (_bagliAyarlarButon == null)
            _bagliAyarlarButon = GetComponent<Button>();
        if (_bagliAyarlarButon == null)
        {
            var go = GameObject.Find(VarsayilanButonAdi);
            if (go != null)
                _bagliAyarlarButon = go.GetComponent<Button>();
        }

        if (_bagliAyarlarButon == null)
        {
            Debug.LogWarning($"[AyarlarButtonAdminPanelAcKapa] Button bulunamadı. Inspector'da 'ayarlarButonu' atayın veya bileşeni '{VarsayilanButonAdi}' üzerine ekleyin.");
            return;
        }

        _bagliAyarlarButon.onClick.RemoveListener(AdminPaneliniAc);
        _bagliAyarlarButon.onClick.AddListener(AdminPaneliniAc);
    }

    void KapatButonunuBagla()
    {
        if (adminAyarlarPaneli == null)
            AdminPaneliniCozumle();
        if (adminAyarlarPaneli == null)
            return;

        var paneldekiButonlar = adminAyarlarPaneli.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < paneldekiButonlar.Length; i++)
        {
            var b = paneldekiButonlar[i];
            if (b == null) continue;
            if (string.Equals(b.gameObject.name, KapatButonAdi, System.StringComparison.OrdinalIgnoreCase))
            {
                _bagliKapatButon = b;
                break;
            }
        }

        if (_bagliKapatButon == null)
            return;

        _bagliKapatButon.onClick.RemoveListener(AdminPaneliniKapat);
        _bagliKapatButon.onClick.AddListener(AdminPaneliniKapat);
    }

    void AdminPaneliniCozumle()
    {
        if (adminAyarlarPaneli != null)
            return;

        var kaydiricilar = FindObjectsByType<AdminSettingsPanelYanKaydirici>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (kaydiricilar != null && kaydiricilar.Length > 0)
        {
            adminAyarlarPaneli = kaydiricilar[0].gameObject;
            return;
        }

        if (!string.IsNullOrEmpty(panelGameObjectAdi))
            adminAyarlarPaneli = GameObject.Find(panelGameObjectAdi);
    }

    /// <summary>İsteğe bağlı: Button OnClick listesinden de bağlanabilir (yalnızca açar).</summary>
    public void AdminPaneliniAc()
    {
        if (adminAyarlarPaneli == null)
            AdminPaneliniCozumle();
        if (adminAyarlarPaneli == null)
        {
            Debug.LogWarning("[AyarlarButtonAdminPanelAcKapa] Admin ayar paneli bulunamadı. Inspector'da 'adminAyarlarPaneli' atayın veya sahnede AdminSettingsPanelYanKaydirici olduğundan emin olun.");
            return;
        }

        adminAyarlarPaneli.SetActive(true);
        PanelGorunurYapVeOneAl();
    }

    /// <summary>İsteğe bağlı: CloseButton OnClick veya kod ile.</summary>
    public void AdminPaneliniKapat()
    {
        if (adminAyarlarPaneli == null)
            AdminPaneliniCozumle();
        if (adminAyarlarPaneli == null)
            return;
        adminAyarlarPaneli.SetActive(false);
    }

    /// <summary>Eski davranış (toggle). Geriye dönük uyumluluk için aç/kapa.</summary>
    public void AdminPaneliniAcKapa()
    {
        if (adminAyarlarPaneli == null)
            AdminPaneliniCozumle();
        if (adminAyarlarPaneli == null)
        {
            Debug.LogWarning("[AyarlarButtonAdminPanelAcKapa] Admin ayar paneli bulunamadı. Inspector'da 'adminAyarlarPaneli' atayın veya sahnede AdminSettingsPanelYanKaydirici olduğundan emin olun.");
            return;
        }

        if (adminAyarlarPaneli.activeSelf)
            AdminPaneliniKapat();
        else
            AdminPaneliniAc();
    }

    void PanelGorunurYapVeOneAl()
    {
        if (adminAyarlarPaneli == null)
            return;
        var rt = adminAyarlarPaneli.transform as RectTransform;
        if (rt != null)
            rt.SetAsLastSibling();
        adminAyarlarPaneli.GetComponent<AdminSettingsPanelYanKaydirici>()?.ZorlaTamGenisAc();
        Canvas.ForceUpdateCanvases();
    }
}
