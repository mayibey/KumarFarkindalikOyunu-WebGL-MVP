using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OyunaBaslaButton : MonoBehaviour
{
    [SerializeField] private string hedefSahne = "06_AdminOyunKopya";

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OyunaBasla);
    }

    void OyunaBasla()
    {
        SceneManager.LoadScene(hedefSahne);
    }
}
