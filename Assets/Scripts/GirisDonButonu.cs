using UnityEngine;
using UnityEngine.SceneManagement;

public class GirisDonButonu : MonoBehaviour
{
    /// <summary>Log ekranındaki butondan giriş sahnesine döner.</summary>
    public void GiriseDon()
    {
        const string girisSahneAdi = "01_GirisScene";
        if (GameManager.I != null)
            GameManager.I.LoadScene(girisSahneAdi);
        else
            SceneManager.LoadScene(girisSahneAdi, LoadSceneMode.Single);
    }
}

