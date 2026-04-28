using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GirisUI : MonoBehaviour
{
    [Header("Oyun Tipi Butonlar")]
    public Button buton_SenaryoluOyun;    // Buton_SenaryoluOyun
    public Button buton_AdminOyun;       // Buton_AdminOyun

    [Header("Kullanıcı Giriş Paneli")]
    public GameObject panel_KullaniciGirisi;  // Panel_KullaniciGirisi (başlangıçta kapalı)
    public TMP_InputField isimInput;      // Kullanıcı adı input alanı
    public Transform listeContent;        // ScrollView/Viewport/Content
    public GameObject kullaniciButonPrefab; // Kullanıcı listesi buton prefab
    public Button btn_GirisYap;            // Giriş Yap butonu

    [Header("Uyarı")]
    public TMP_Text uyariText;            // Uyarı mesajları

    [Header("Sahne Adları")]
    public string senaryoluOyunSceneAdi = "02_SenaryoluOyun";
    public string adminOyunSceneAdi = "03_AdminOyunScene";

    [Header("Hoş Geldiniz (opsiyonel, eski HosgeldinizText)")]
    public TMP_Text hosgeldinizText;
    public string hosgeldinizMesaj = "Hoş Geldiniz!";

    private List<PlayerProfile> _profilesCache = new List<PlayerProfile>();
    private string _secilenOyunTipi = ""; // "SENARYOLU" veya "ADMIN"

    void Start()
    {
        // Scene'deki butonları otomatik bul
        buton_SenaryoluOyun = GameObject.Find("Buton_SenaryoluOyun")?.GetComponent<Button>();
        buton_AdminOyun = GameObject.Find("Buton_AdminOyun")?.GetComponent<Button>();
        
        // Panel başlangıçta gizli
        if (panel_KullaniciGirisi) 
            panel_KullaniciGirisi.SetActive(false);

        // Buton eventlerini bağla
        if (buton_SenaryoluOyun) 
            buton_SenaryoluOyun.onClick.AddListener(() => OyunTipiSecildi("SENARYOLU"));
        
        if (buton_AdminOyun) 
            buton_AdminOyun.onClick.AddListener(() => OyunTipiSecildi("ADMIN"));

        if (btn_GirisYap) 
            btn_GirisYap.onClick.AddListener(GirisYap);

        if (isimInput) 
            isimInput.onValueChanged.AddListener(_ => ListeyiYenile());

        if (uyariText) 
            uyariText.text = "";

        HosgeldinizYazdir();
        CacheYukle();
        
        Debug.Log("GirisUI başlatıldı. Butonlar otomatik bağlandı.");
    }

    void OnEnable()
    {
        HosgeldinizYazdir();
    }

    void HosgeldinizYazdir()
    {
        if (hosgeldinizText != null)
            hosgeldinizText.text = hosgeldinizMesaj;
    }

    void OyunTipiSecildi(string oyunTipi)
    {
        _secilenOyunTipi = oyunTipi;
        
        // Kullanıcı giriş panelini göster
        if (panel_KullaniciGirisi) 
            panel_KullaniciGirisi.SetActive(true);

        // Listeyi güncelle
        ListeyiYenile();
        
        Debug.Log(oyunTipi + " oyunu seçildi. Kullanıcı girişi bekleniyor.");
    }

    void GirisYap()
    {
        if (uyariText) uyariText.text = "";

        if (GameManager.I == null)
        {
            if (uyariText) uyariText.text = "GameManager bulunamadı.";
            return;
        }

        string isim = (isimInput ? isimInput.text : "").Trim();

        if (string.IsNullOrEmpty(isim))
        {
            if (uyariText) uyariText.text = "Lütfen isim girin.";
            return;
        }

        if (isim.Length < 2)
        {
            if (uyariText) uyariText.text = "İsim en az 2 karakter olmalı.";
            return;
        }

        // Oyuncu seç veya oluştur
        GameManager.I.SelectOrCreatePlayer(isim);

        // İlgili sahneye git
        string hedefSahne = (_secilenOyunTipi == "ADMIN") 
            ? adminOyunSceneAdi 
            : senaryoluOyunSceneAdi;

        GameManager.I.LoadScene(hedefSahne);
        Debug.Log(isim + " giriş yaptı. Hedef sahne: " + hedefSahne);
    }

    void CacheYukle()
    {
        if (GameManager.I == null)
        {
            Debug.LogWarning("GameManager yok. GirisScene'de GameManager olmalı.");
            _profilesCache = new List<PlayerProfile>();
            return;
        }

        _profilesCache = GameManager.I.Profiles != null
            ? new List<PlayerProfile>(GameManager.I.Profiles)
            : new List<PlayerProfile>();
    }

    void ListeyiYenile()
    {
        if (listeContent == null || kullaniciButonPrefab == null) return;

        // Mevcut butonları temizle
        for (int i = listeContent.childCount - 1; i >= 0; i--)
            Destroy(listeContent.GetChild(i).gameObject);

        string q = (isimInput ? isimInput.text : "").Trim().ToLowerInvariant();

        // Kullanıcıları filtrele ve sırala
        IEnumerable<PlayerProfile> list = _profilesCache
            .OrderBy(p => p.playerName, StringComparer.InvariantCultureIgnoreCase);

        if (!string.IsNullOrEmpty(q))
            list = list.Where(p => (p.playerName ?? "").ToLowerInvariant().Contains(q));

        // Kullanıcı butonları oluştur
        foreach (var p in list)
        {
            GameObject go = Instantiate(kullaniciButonPrefab, listeContent);

            Button b = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
            TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);

            if (t) t.text = p.playerName;

            if (b != null)
            {
                string secilecekIsim = p.playerName;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => KullaniciListedenSec(secilecekIsim));
            }
        }
    }

    void KullaniciListedenSec(string isim)
    {
        if (GameManager.I == null) return;

        // Mevcut kullanıcıyı seç
        GameManager.I.SelectOrCreatePlayer(isim);

        // İlgili sahneye git
        string hedefSahne = (_secilenOyunTipi == "ADMIN") 
            ? adminOyunSceneAdi 
            : senaryoluOyunSceneAdi;

        GameManager.I.LoadScene(hedefSahne);
        Debug.Log(isim + " seçildi. Hedef sahne: " + hedefSahne);
    }
}
