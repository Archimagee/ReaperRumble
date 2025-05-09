using UnityEngine;



public class Instructions : MonoBehaviour
{
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (this.gameObject.activeInHierarchy == true) this.gameObject.SetActive(false);
        }
    }
}
