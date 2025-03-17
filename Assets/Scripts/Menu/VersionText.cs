using TMPro;
using UnityEngine;



public class VersionText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _versionText;
    [SerializeField] private string _version;
    public static string Version;



    void Start()
    {
        Version = _version;
        if (_versionText != null) _versionText.text = "Version - " + Version;
    }
}
