using THPS.Singletons;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

struct GlobalData : IComponentData
{
    public float Value1;
    public float Value2;
}

struct GlobalData2 : IComponentData
{
    public float4x4 Matrix1;
    public float4x4 Matrix2;
}

struct GameModeState : IComponentData
{
    public float Value3;
}

struct TestComponent : IComponentData { }

[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
partial class ManagedSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Normal
        EntityManager.CreateSingleton<GlobalData2>();
        EntityManager.CreateSingleton<GameModeState>();
        EntityManager.CreateSingleton(new GlobalData
        {
            Value1 = 1.0f,
        });

        // One entity, better
        //SingletonUtilities.Setup(EntityManager);
        //EntityManager.CreateOrAddSingleton<GlobalData2>();
        //EntityManager.CreateOrAddSingleton<GameModeState>();
        //EntityManager.CreateOrSetSingleton(new GlobalData
        //{
        //    Value1 = 1.0f,
        //});
    }

    protected override void OnUpdate()
    {
        //var globalData = EntityManager.GetSingleton<GlobalData>();
        var globalData = SystemAPI.GetSingleton<GlobalData>();

        // No problem, just data
        Entities
            .WithName("Run_Read")
            .ForEach((ref LocalTransform trans) =>
        {
            UnityEngine.Debug.Log("Run_Read");
            if (trans.Position.y > globalData.Value1)
            {
                // Do something
            }

        }).Run();

        // No problem, just data
        Entities
            .WithName("ScheduleParallel_Read")
            .ForEach((ref LocalTransform trans) =>
        {
            UnityEngine.Debug.Log("ScheduleParallel_Read");
            if (trans.Position.y > globalData.Value1)
            {
                // Do something
            }

        }).ScheduleParallel();

        var globalDataRW = SystemAPI.GetSingletonRW<GlobalData>();

        // No problem, main thread
        Entities
            .WithName("Run_Write1")
            .ForEach((ref LocalTransform trans) =>
        {
            UnityEngine.Debug.Log("Run_Write1");
            globalDataRW.ValueRW.Value2 = trans.Scale;

        }).Run();

        // Structural change
        //var defSingletonEntity = EntityManager.GetDefaultSingletonEntity();
        //if (SystemAPI.HasComponent<TestComponent>(defSingletonEntity))
        //    EntityManager.RemoveComponent<TestComponent>(defSingletonEntity);
        //else
        //    EntityManager.AddComponent<TestComponent>(defSingletonEntity);

        // Fix
        // globalDataRW = SystemAPI.GetSingletonRW<GlobalData>();

        // Error
        // ObjectDisposedException: Attempted to access ComponentTypeHandle<GlobalData> which has been invalidated by a structural change.
        Entities
            .WithName("Run_Write2")
            .ForEach((ref LocalTransform trans) =>
        {
            UnityEngine.Debug.Log("Run_Write2");
            globalDataRW.ValueRW.Value2 = trans.Scale;

        }).Run();


        // PROBLEM, even if allowed, a cut race condition with main thread
        Entities
            .WithName("Schedule_Write")
            .ForEach((ref LocalTransform trans) =>
        {
            //globalDataRW.ValueRW.Value2 = trans.Scale;

        }).Schedule();

        // PROBLEM, even if allowed, a cut race condition with parellel threads
        Entities
            .WithName("ScheduleParallel_Write")
            .ForEach((ref LocalTransform trans) =>
        {
            //globalDataRW.ValueRW.Value2 = trans.Scale;

        }).ScheduleParallel();
    }
}

[BurstCompile]
[DisableAutoCreation]
partial struct UnmanagedSystem : ISystem
{
    private NativeQueue<byte> Data;

    [BurstCompile] // Writing to a static field in burst is not allowed
    public void OnCreate(ref SystemState state)
    {
        //SingletonUtilities.Setup(state.EntityManager);
        Data = new NativeQueue<byte>(Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        Data.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (Data.Count > 0)
        {
            UnityEngine.Debug.Log("Change exported from Parallel WriteJob");
            SystemAPI.GetSingletonRW<GlobalData>().ValueRW.Value2 = 4;
        }

        var globalData = SystemAPI.GetSingleton<GlobalData>();

        foreach (var trans in SystemAPI.Query<LocalTransform>())
        {
            if (trans.Position.y > globalData.Value1)
            {
                // Do something
            }
        }

        foreach (var (gameModeState, data1, data2) in SystemAPI.Query<GameModeState, GlobalData, GlobalData2>())
        {
            UnityEngine.Debug.Log("GameModeState, GlobalData and GlobalData2 is on the same Entity");
        }

        foreach (var gameModeState in SystemAPI.Query<GameModeState>().WithNone<GlobalData>())
        {
            var data12 = SystemAPI.GetSingleton<GlobalData>();
            var data2 = SystemAPI.GetSingleton<GlobalData2>();
            UnityEngine.Debug.Log("GameModeState, GlobalData and GlobalData2 is NOT on the same Entity");
        }

        Data.Clear();

        state.Dependency = new WriteJob_Correct
        {
            GlobalData = globalData,
            OutData = Data.AsParallelWriter(),

        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    partial struct WriteJob_Correct : IJobEntity
    {
        public GlobalData GlobalData;

        [WriteOnly]
        public NativeQueue<byte>.ParallelWriter OutData;

        public void Execute(ref LocalTransform trans)
        {
            if (trans.Position.y > GlobalData.Value1)
            {
                OutData.Enqueue(1);

                // Do something
            }
        }
    }

    [BurstCompile]
    partial struct WriteJob_Problematic : IJobEntity
    {
        public Entity DefaultSingleton;
        public GlobalData GlobalData;

        [WriteOnly]
        public EntityCommandBuffer.ParallelWriter CmdPW;

        public void Execute([EntityIndexInQuery] int index, ref LocalTransform trans)
        {
            if (trans.Position.y > GlobalData.Value1)
            {
                GlobalData.Value2 = 4;
                CmdPW.SetComponent(index, DefaultSingleton, GlobalData);

                // Do something
            }
        }
    }
}
