using UnityEngine;

public class FaceMainCamera : MonoBehaviour
{
    [SerializeField] private bool _reverse;
    void LateUpdate()
    {
        if (_reverse) transform.forward = Camera.main.transform.forward;
        else transform.forward = -Camera.main.transform.forward;
    }
}