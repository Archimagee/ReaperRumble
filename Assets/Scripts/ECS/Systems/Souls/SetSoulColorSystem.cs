using Unity.Burst;
using Unity.Entities;
using UnityEngine.VFX;
using UnityEngine;
using static Unity.Entities.SystemAPI.ManagedAPI;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(ChangeSoulGroupServerSystem))]
public partial class SetSoulColorSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SoulColorChanged>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach ((UnityEngineComponent<VisualEffect> vfx, RefRO<SoulGroupWasChanged> groupChange, RefRW<SoulColorChanged> colorChange, RefRO<SoulGroupMember> soulGroup, Entity soulEntity)
            in SystemAPI.Query<UnityEngineComponent<VisualEffect>, RefRO<SoulGroupWasChanged>, RefRW<SoulColorChanged>, RefRO<SoulGroupMember>>().WithEntityAccess())
        {
            if (groupChange.ValueRO.WasChangedAt > colorChange.ValueRO.WasChangedAt)
            {
                Vector4 color;

                if (SystemAPI.GetComponent<SoulGroupTarget>(soulGroup.ValueRO.MyGroup).MyTarget == Entity.Null) color = new Vector4(0.32f, 0.2f, 0.7f, 1f);
                else color = SystemAPI.GetComponent<PlayerData>(SystemAPI.GetComponent<SoulGroupTarget>(soulGroup.ValueRO.MyGroup).MyTarget).MyColour;

                vfx.Value.SetVector4("SoulColor", color);

                colorChange.ValueRW.WasChangedAt = SystemAPI.Time.ElapsedTime;
            }
        }
    }
}

public partial struct SoulColorChanged : IComponentData
{
    public double WasChangedAt;
}