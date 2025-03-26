using UnityEngine;
using UnityEngine.Events;



public class PresentationGameObjectDestructionManager : MonoBehaviour
{
    [SerializeField] private UnityEvent _eventsOnDestruction;
    [SerializeField] private float _delay;



    public void Destroy(GameObject gameObject)
    {
        _eventsOnDestruction.Invoke();
        Destroy(gameObject, _delay);
    }
}