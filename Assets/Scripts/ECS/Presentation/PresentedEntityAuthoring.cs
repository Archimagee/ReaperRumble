using Unity.Entities;
using UnityEngine;



public class PresentedEntityAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _presentationPrefab;



    public class Baker : Baker<PresentedEntityAuthoring>
    {
        public override void Bake(PresentedEntityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.WorldSpace);
            AddComponentObject(entity, new PresentationGameObjectPrefab()
            {
                Prefab = authoring._presentationPrefab
            });
        }
    }
}


public class PresentationGameObjectPrefab : IComponentData
{
    public GameObject Prefab;
}

public class PresentationGameObjectCleanup : ICleanupComponentData
{
    public GameObject Instance;
}