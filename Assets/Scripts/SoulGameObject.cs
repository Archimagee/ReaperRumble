using UnityEngine;



public class SoulGameObject : MonoBehaviour
{
    private SoulBoidController _myController;
    private GameObject _target;
    private static float _separation = 0.1f;

    private Vector3 FacingDirection = Vector3.forward;



    public void Setup(SoulBoidController controller, GameObject target)
    {
        _myController = controller;
        _target = target;
    }



    public void FixedUpdate()
    {
        Vector3 distanceToMove = Vector3.zero;
        Vector3 currentPos = this.transform.position;

        Vector3 direction = (_target.transform.position - currentPos).normalized;
        float distance = Vector3.Distance(currentPos, _target.transform.position);
        float speed = 0.07f + (distance / 30);
        FacingDirection = Vector3.RotateTowards(FacingDirection, direction, Mathf.PI / 25, 1000f).normalized;

        foreach (SoulGameObject soul in _myController.CurrentSouls)
        {
            if (soul != this)
            {
                direction = (soul.transform.position - currentPos).normalized;
                distance = Vector3.Distance(currentPos, soul.transform.position);
                Vector3 newDirectionTowards = Vector3.RotateTowards(FacingDirection, direction, Mathf.PI / 100, 1000f).normalized;
                FacingDirection += (FacingDirection - newDirectionTowards) / distance;
                distanceToMove = (-direction / distance) * _separation;
            }
        }

        this.transform.position += distanceToMove;
        this.transform.position += FacingDirection * speed;
    }
}