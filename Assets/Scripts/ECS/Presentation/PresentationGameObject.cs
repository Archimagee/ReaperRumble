using UnityEngine;
using Unity.Entities;



public class PresentationGameObject : MonoBehaviour
{
    private Entity _entityToPresent;
    private World _world;



    public void Assign(Entity entity, World world)
    {
        _entityToPresent = entity;
        _world = world;
    }

    public Entity GetEntity()
    {
        return _entityToPresent;
    }



    public void OnDestroy()
    {
        if (_entityToPresent != Entity.Null && _world.IsCreated && _world.EntityManager.Exists(_entityToPresent)) _world.EntityManager.DestroyEntity(_entityToPresent); 
    }
}