using TMPro;
using UnityEngine;

public class SetText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _text;



    public void Set(string text)
    {
        _text.text = text;
    }
}
