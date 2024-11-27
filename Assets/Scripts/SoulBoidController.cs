using System.Collections.Generic;
using UnityEngine;



public class SoulBoidController : MonoBehaviour
{
    [SerializeField] private SoulGameObject _soulPrefab;

    private List<SoulGameObject> _currentSouls = new List<SoulGameObject>();
    public List<SoulGameObject> CurrentSouls { get { return _currentSouls; } }



    public void ClearSouls()
    {
        _currentSouls.Clear();
    }



    public void Start()
    {
        for (int x = 0; x < 10; x++) _currentSouls.Add(GameObject.Instantiate(_soulPrefab, Vector3.zero, Quaternion.identity));
    }
}
