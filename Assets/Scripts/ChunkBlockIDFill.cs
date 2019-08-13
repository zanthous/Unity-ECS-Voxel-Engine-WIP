
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using System;
using Unity.Mathematics;
using UnityEngine;

//ChunkManager?

public class ChunkBlockIDFill : JobComponentSystem
{
    EntityQuery query;
    EntityQuery playerQuery;
    //private int renderX = Settings.RenderDistance * 2;
    //private int renderY = Settings.WorldHeight / Settings.ChunkSize;
    //private int renderZ = Settings.RenderDistance * 2;

    Player playerRef;

    //I don't know what thing to use here so I took a random one
    //What I should use may be written here somewhere:
    //https://docs.unity3d.com/Packages/com.unity.entities@0.0/api/Unity.Entities.EntityCommandBufferSystem.html

    //Component system update ordering is now hierarchical. A forthcoming document will cover this feature in detail. Key changes:
    //Added ComponentSystemGroup class, representing a group of systems (and system groups) to update in a fixed order.
    //The following ComponentSystemGroups are added to the Unity player loop by default:
    //InitializationSystemGroup (in the Initialization phase)
    //SimulationSystemGroup (in the FixedUpdate phase)
    //PresentationSystemGroup (in the Update phase)
    //..etc taken from:
    //https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/ReleaseNotes.md#changes-6
    //From my best guess from the text above this would be what I use
    //DelayedChunkTaggingBufferSystem system;
    //Using this had no apparent effect so I will return to the other version I had
    //DelayedChunkTaggingBufferSystem system;
    //Actually trying one more time while adding a updateingroup tag to this system
    //No noticeable effect
    //Temporarily just returning from the execute function if accidentally called into again
    //Definitely need to fix this later somehow


    DelayedChunkTaggingBufferSystem system;

    protected override void OnCreateManager()
    {
        query = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[] {
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadOnly<Chunk>(),
                ComponentType.ReadOnly<Ungenerated>(),
                ComponentType.ReadWrite<BlockIDBuffer>()
            },
            Any = System.Array.Empty<ComponentType>(),
            None = System.Array.Empty<ComponentType>()
        });
        playerQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Player>(),
                ComponentType.ReadOnly<Translation>()
            }
        });

        system = World.Active.GetOrCreateSystem<DelayedChunkTaggingBufferSystem>();
    }

    protected override void OnDestroyManager()
    {
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //TODO
        var players = playerQuery.ToComponentDataArray<Player>(Allocator.TempJob);
        playerRef = players[0];
        float3 playerPosition = playerRef.position;
        int renderDistance = playerRef.renderDistance;
        players.Dispose();

        //var player = get
        //var pos = GetComponentDataFromEntity<Player>();

        //if(playerRef.Length>1)
        //{
        //    Debug.Log("More than 1 player found! - ChunkIterationTest.cs");
        //}
        //else if(playerRef.Length == 0)
        //{
        //    Debug.Log("No player found! - ChunkIterationTest.cs");
        //}

        //AddJobHandleForProducer
        var job = new InitializeChunkBufferJob
        {
            blocks = GetBufferFromEntity<BlockIDBuffer>(false),
            PlayerPos = playerPosition,
            RenderDistance = playerRef.renderDistance,
            CommandBuffer = system.CreateCommandBuffer().ToConcurrent()
        };
        inputDeps = job.Schedule<InitializeChunkBufferJob>(query, inputDeps);
        system.AddJobHandleForProducer(inputDeps);
        return inputDeps;
    }



    //Takes the player's current position and converts into chunk coordinates by dividing by chunk size
    //then it loads chunks around the player in a renderdistance radius
    //Move Chunk from requirecomponenttag to IJobForEachWithEntity<Translation> if ever needed to access

    //NOTE: Cannot use burstcompile with entitycommandbuffer 7/18/2019
    //NOTE: If I add teleportation, then this needs to be adjusted
    //I think I can use these instead of queries?
    //[RequireComponentTag(, typeof(BlockIDBuffer), typeof(Chunk))]
    unsafe struct InitializeChunkBufferJob : IJobForEachWithEntity<Translation, Ungenerated>
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<BlockIDBuffer> blocks;
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public int RenderDistance;
        //Settings.RenderDistance * 2 * Settings.WorldHeight
        //x = mod width
        public EntityCommandBuffer.Concurrent CommandBuffer;
        //Proof of concept being done with renderdistance = 4
        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref Ungenerated ungenerated)
        {
            ////Running into an issue currently where the entitycommandbuffer isn't properly removing
            ////generated chunks out of this job 
            //if(blocks[entity].Length !=0)
            //{
            //    Debug.Log("Error! Chunk already initialized!");
            //    return;
            //}

            //First init
            if(blocks[entity].Length == 0)
            {
                Translation chunkPos = new Translation();
                //x increments every index but wraps every renderdistance*2 blocks
                //Offset by render distance so the player is in the middle though
                chunkPos.Value.x = ((int) PlayerPos.x + index % (RenderDistance * 2)) - RenderDistance;
                //y increments everytime an x-z layer is made (renderdistance*2)^2 or 2^6 and does not wrap
                chunkPos.Value.y = index / ((RenderDistance * 2) * (RenderDistance * 2)); //
                                                                                          //z increments every time x completes a row, so every 8 blocks (renderdistance*2, which is 2^3)
                                                                                          //z wraps every renderdistance*2 blocks
                chunkPos.Value.z = ((int) PlayerPos.z + (index / (RenderDistance * 2)) % (RenderDistance * 2)) - RenderDistance;
                //Chunk pos is the position of a chunk in the world determined by the index in the job

                //multiply by size of chunk to get location in units
                translation.Value = chunkPos.Value * 16;

                //blocks[entity].Reserve(16 * 16 * 16);

                ushort x, y, z;
                int height;

                ushort test;
                for(ushort i = 0; i < 16 * 16 * 16; i++)
                {
                    //x = i & 15;
                    //y = i >> 8;
                    //z = (i >> 4) & 15;
                    MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
                    height = (32 + (int) (Mathf.PerlinNoise((translation.Value.x + x) * 0.015625f, (translation.Value.z + z) * 0.015625f) * 70.0f));
                    blocks[entity].Add(((chunkPos.Value.y * 16 + y) < height ? (ushort) 0 : (ushort) (2000)));
                }
                CommandBuffer.RemoveComponent<Ungenerated>(index, entity);
            }
            else
            {
                //TODO 
                blocks[entity].Clear();

                //move over renderdistance+1
                if(ungenerated.xtruezfalse)
                {
                    if(translation.Value.x > PlayerPos.x)
                    {
                        translation.Value.x -= (RenderDistance * 2) * 16;
                        //y unchanged
                        //z unchanged
                    }
                    else
                    {
                        translation.Value.x += (RenderDistance * 2) * 16;
                        //y unchanged
                        //z unchanged
                    }
                }
                else
                {
                    if(translation.Value.z > PlayerPos.z)
                    {
                        translation.Value.z -= (RenderDistance * 2) * 16;
                    }
                    else
                    {
                        translation.Value.z += (RenderDistance * 2) * 16;
                    }

                    //Debug.Log("I fucked up");
                    //Debug.Log("Trans x: " + translation.Value.x + "\n Player chunkX scaled up: " + (ChunkPos.x * 16));
                    //Debug.Log("Trans z: " + translation.Value.x + "\n Player chunkZ scaled up: " + (ChunkPos.z * 16));
                    //Debug.Log("x result: " + Mathf.Abs(translation.Value.x - (PlayerPos.x * 16.0f)));
                    //Debug.Log("z result: " + Mathf.Abs(translation.Value.z - (PlayerPos.z * 16.0f)));
                }
                //translation.Value = chunkPos.Value * 16;

                //blocks[entity].Reserve(16 * 16 * 16);

                ushort x, y, z;
                int height;

                for(ushort i = 0; i < 16 * 16 * 16; i++)
                {
                    //x = i & 15;
                    //y = i >> 8;
                    //z = (i >> 4) & 15;
                    MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
                    height = (32 + (int) (Mathf.PerlinNoise((translation.Value.x + x) * 0.015625f, (translation.Value.z + z) * 0.015625f) * 70.0f));
                    blocks[entity].Add(((translation.Value.y + y) < height ? (ushort) 0 : (ushort) (2000)));
                }
                CommandBuffer.RemoveComponent<Ungenerated>(index, entity);
            }
        }
    }
}


public class DelayedChunkTaggingBufferSystem : EntityCommandBufferSystem
{

}