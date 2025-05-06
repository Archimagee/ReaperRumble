using Unity.Entities;
using UnityEngine;



public class LocalPresentedEntityAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _localPresentationPrefab;



    public class Baker : Baker<LocalPresentedEntityAuthoring>
    {
        public override void Bake(LocalPresentedEntityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.WorldSpace);
            AddComponentObject(entity, new LocalPresentationGameObjectPrefab()
            {
                Prefab = authoring._localPresentationPrefab
            });
        }
    }
}


public class LocalPresentationGameObjectPrefab : IComponentData
{
    public GameObject Prefab;
}