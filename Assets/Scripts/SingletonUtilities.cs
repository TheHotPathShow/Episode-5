using System;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace THPS.Singletons
{
    public struct DefaultSingleton : IComponentData { }

    public static class SingletonUtilities
    {
        private static Entity _SingletonEntity;

        public static void Setup(EntityManager entityManager)
        {
            if (entityManager.Exists(_SingletonEntity))
            {
                UnityEngine.Debug.LogError("DefaultSingleton already created!");
                return;
            }

            _SingletonEntity = entityManager.CreateEntity();
            entityManager.AddComponent<DefaultSingleton>(_SingletonEntity);
        }

        public static Entity GetDefaultSingletonEntity(this EntityManager entityManager)
        {
            return _SingletonEntity;
        }

        public static bool HasSingleton<T>(this EntityManager entityManager) where T : struct, IComponentData
        {
            return entityManager.HasComponent<T>(_SingletonEntity);
        }

        public static T GetSingleton<T>(this EntityManager entityManager) where T : unmanaged, IComponentData
        {
            return entityManager.GetComponentData<T>(_SingletonEntity);
        }

        public static Entity CreateOrSetSingleton<T>(this EntityManager entityManager, T data) where T : unmanaged, IComponentData
        {
            if (entityManager.HasComponent<T>(_SingletonEntity))
                entityManager.SetComponentData(_SingletonEntity, data);
            else
                entityManager.AddComponentData(_SingletonEntity, data);

            return _SingletonEntity;
        }

        public static Entity CreateOrAddSingleton<T>(this EntityManager entityManager) where T : IComponentData
        {
            if (entityManager.HasComponent<T>(_SingletonEntity) == false)
                entityManager.AddComponent<T>(_SingletonEntity);

            return _SingletonEntity;
        }

        public static void RemoveSingletonComponentIfExists<T>(this EntityManager entityManager) where T : IComponentData
        {
            if (entityManager.HasComponent<T>(_SingletonEntity))
            {
                entityManager.RemoveComponent<T>(_SingletonEntity);
            }
        }

        public static void RemoveSingletonBufferIfExists<T>(this EntityManager entityManager) where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasBuffer<T>(_SingletonEntity))
            {
                entityManager.RemoveComponent<T>(_SingletonEntity);
            }
        }

        public static DynamicBuffer<T> GetOrCreateSingletonBuffer<T>(this EntityManager entityManager) where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasComponent<T>(_SingletonEntity))
            {
                return entityManager.GetBuffer<T>(_SingletonEntity);
            }
            else
            {
                return entityManager.AddBuffer<T>(_SingletonEntity);
            }
        }

        public static DynamicBuffer<T> CreateOrResetSingletonBuffer<T>(this EntityManager entityManager) where T : unmanaged, IBufferElementData
        {
            if (entityManager.HasComponent<T>(_SingletonEntity))
            {
                var buffer = entityManager.GetBuffer<T>(_SingletonEntity);
                buffer.Clear();
                return buffer;
            }
            else
            {
                return entityManager.AddBuffer<T>(_SingletonEntity);
            }
        }

        public static bool HasSingletonBuffer<T>(this EntityManager entityManager) where T : unmanaged, IBufferElementData
        {
            return entityManager.HasComponent<T>(_SingletonEntity);
        }

        public static bool TryGetSingleton<T>(this EntityManager entityManager, out T data) where T : unmanaged, IComponentData
        {
            data = default;
            if (entityManager.HasComponent<T>(_SingletonEntity))
            {
                data = entityManager.GetComponentData<T>(_SingletonEntity);
                return true;
            }

            return false;
        }


        //public static unsafe RefRW<T> GetSingletonRW<T>(this EntityManager entityManager) where T : unmanaged, IComponentData
        //{
        //    var typeIndex = TypeManager.GetTypeIndex<T>();

        //    var chunk = entityManager.GetChunk(_SingletonEntity);
        //    GetSingletonChunk(typeIndex, out var indexInArchetype, out chunk);

        //    var data = ChunkDataUtility.GetComponentDataRW(chunk, 0, indexInArchetype, entityManager.GlobalSystemVersion);

        //    entityManager.CompleteDependencyBeforeRW<T>();
        //    return new RefRW<T>(data, default);
        //}
    }
}