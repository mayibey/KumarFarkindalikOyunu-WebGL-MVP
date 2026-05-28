using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OyunaBaslaButton : MonoBehaviour
{
    [SerializeField] private string hedefSahne = "02_SenaryoluOyun"; // FAZ35.77: eski 03 → 02 rename.

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
