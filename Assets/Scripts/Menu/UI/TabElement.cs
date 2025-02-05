using UnityEngine;
using UnityEngine.UI;



[RequireComponent (typeof(Button))]
public class TabElement : MonoBehaviour
{
    [SerializeField] private GameObject _tabGameObject;


    public delegate void TabDelegate(TabElement tab);
    public TabDelegate RaiseTabSelected;
}
