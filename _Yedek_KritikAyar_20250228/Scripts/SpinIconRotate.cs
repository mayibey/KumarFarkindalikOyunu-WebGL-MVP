using UnityEngine;

public class SpinIconRotate : MonoBehaviour
{
    private bool _dönüyor = false;
    private float _dönüşHızı = 360f; // 1 tur = 360 derece
    private float _dönenAçı = 0f;
    
    public void SetRotate(bool on)
    {
        if (on)
        {
            _dönüyor = true;
            _dönenAçı = 0f;
        }
        else
        {
            _dönüyor = false;
        }
    }

    void Update()
    {
        if (_dönüyor && _dönenAçı < 360f)
        {
            float buFrame = _dönüşHızı * Time.deltaTime;
            _dönenAçı += buFrame;
            
            // 360' ı geçerse360'a sabitle
            if (_dönenAçı >= 360f)
            {
                buFrame -= (_dönenAçı - 360f);
                _dönüyor = false;
            }
            
            transform.Rotate(0, 0, -buFrame);
        }
    }
}
