using Unity.Entities;



public struct ChangeSoulGroupPending : IComponentData
{
    public Entity SoulEntity;
    public Entity GroupToChangeTo;
}