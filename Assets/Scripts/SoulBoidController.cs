using System.Collections.Generic;
using UnityEngine;



public class SoulBoidController : MonoBehaviour
{
    [SerializeField] private SoulGameObject _soulPrefab;
    [SerializeField] private GameObject _target;
    [SerializeField] private GameObject _follower;

    private List<SoulGameObject> _currentSouls = new List<SoulGameObject>();
    public List<SoulGameObject> CurrentSouls { get { return _currentSouls; } }



    public void ClearSouls()
    {
        _currentSouls.Clear();
    }



    public void FixedUpdate()
    {
        Vector3 targetPos = _target.transform.position;
        Vector3 followerPos = _follower.transform.position;
        Vector3 direction = (targetPos - followerPos).normalized;
        float distance = Vector3.Distance(followerPos, targetPos);
        distance = Mathf.Max(distance - 8f, 0f);
        _follower.transform.position += direction * distance * 0.05f;
    }



    public void Start()
    {
        for (int x = 0; x < 5; x++)
        {
            SoulGameObject newSoul = GameObject.Instantiate(_soulPrefab, Vector3.zero, Quaternion.identity, this.transform);
            newSoul.Setup(this, _follower);
            newSoul.transform.position += new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));
            _currentSouls.Add(newSoul);
        }
    }
}
