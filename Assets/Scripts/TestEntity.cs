using Unity.Entities;
using UnityEngine;

public class TestEntity : MonoBehaviour
{
    class Baker : Baker<TestEntity>
    {
        public override void Bake(TestEntity authoring)
        {
            var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent<TestEntityComponent>(entity);
        }
    }
}

struct TestEntityComponent : IComponentData { }