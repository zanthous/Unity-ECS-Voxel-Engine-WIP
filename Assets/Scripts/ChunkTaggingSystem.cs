using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[DisableAutoCreation]
public class ChunkTaggingSystem : JobComponentSystem
{
    public class TagAsUngeneratedECBS : EntityCommandBufferSystem { }

    EntityQuery query;
    EntityQuery playerQuery;
    private TagAsUngeneratedECBS system;
    private GameObject camera;
    
    protected override void OnCreate()
    {
        camera = GameObject.FindObjectOfType<Camera>().gameObject;

        query = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Translation>(),
                //Only move complete chunks otherwise chunk errors pop up
                //since im moving chunks while they are generating and they dont restart their generation
                //ComponentType.ReadOnly<Chunk>(),
                ComponentType.ReadOnly<ChunkUpToDate>()
            },
            Any = System.Array.Empty<ComponentType>(),
            None = new ComponentType[]
            {
                ComponentType.ReadOnly<Ungenerated>()
            }
        });
        playerQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadWrite<Player>(), 
                ComponentType.ReadWrite<Translation>()
            }
        });

        system = World.Active.GetOrCreateSystem<TagAsUngeneratedECBS>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new UpdatePlayerPos
        {
            Position = camera.transform.position
        };
        var job2 = new TagChunksUngenerated
        {
            Translation = GetArchetypeChunkComponentType<Translation>(),
            PlayerPos = camera.transform.position,
            entityType = GetArchetypeChunkEntityType(),
            CommandBuffer = system.CreateCommandBuffer().ToConcurrent(),
            RenderDistance = Settings.RenderDistance
        };

        //TODO potential issue here
        var h1 = job.Schedule<UpdatePlayerPos>(playerQuery, inputDeps);

        var h2 = job2.Schedule<TagChunksUngenerated>(query, h1);

        system.AddJobHandleForProducer(h2);

        return JobHandle.CombineDependencies(h1,h2);
    }

    //TODO requires change if chunk size changed

    //[BurstCompile]
    struct UpdatePlayerPos : IJobForEach<Player,Translation>
    {
        [ReadOnly] public float3 Position;
        public void Execute(ref Player player, ref Translation translation)
        {
            player.chunkX = (int) Position.x >> 4;
            player.chunkY = (int) Position.y >> 4;
            player.chunkZ = (int) Position.z >> 4;

            player.position = Position;
            //Debug.Log("x: " + ((int) Position.x >> 4));
            translation.Value = Position;
        }
    }

    //TODO make update with render distance
    struct TagChunksUngenerated : IJobChunk
    {
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public int RenderDistance;

        //need translation
        [ReadOnly] public ArchetypeChunkComponentType<Translation> Translation;
        [ReadOnly] public ArchetypeChunkEntityType entityType;

        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var translations = chunk.GetNativeArray(Translation);
            var entities = chunk.GetNativeArray(entityType);

            //Debug.Log(r1 + " " + r2);
            int3 chunkOffset = new int3(0, 0, 0);

            if(Mathf.Abs(PlayerPos.x - translations[0].Value.x) > RenderDistance * 16)
            {
                if(PlayerPos.x > translations[0].Value.x)
                {
                    chunkOffset.x += RenderDistance * 2;
                }
                else
                {
                    chunkOffset.x -= RenderDistance * 2;
                }
            }
            if(Mathf.Abs(PlayerPos.z - translations[0].Value.z) > RenderDistance * 16 )
            {
                //if()
                if(PlayerPos.z > translations[0].Value.z)
                {
                    chunkOffset.z += RenderDistance * 2;
                }
                else
                {
                    chunkOffset.z -= RenderDistance * 2;
                }
            }
            if(chunkOffset.x != 0 || chunkOffset.z != 0)
            {
                CommandBuffer.AddComponent<Ungenerated>(chunkIndex, entities[0], new Ungenerated { offset = chunkOffset });
                CommandBuffer.RemoveComponent<ChunkUpToDate>(chunkIndex, entities[0]);
            }
        }
    }
}
