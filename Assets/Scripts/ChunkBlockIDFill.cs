
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using System;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.CompilerServices;

//ChunkManager?

public class ChunkBlockIDFill : JobComponentSystem
{
    EntityQuery fillQuery;
    EntityQuery playerQuery;
    EntityQuery positionQuery;
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

    private NativeArray<byte> perm;

    protected override void OnCreate()
    {
        fillQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Chunk>(),
                ComponentType.ReadOnly<Ungenerated>(),
                ComponentType.ReadWrite<BlockIDBuffer>()
            },
            Any = System.Array.Empty<ComponentType>(),
            None = System.Array.Empty<ComponentType>()
        });
        positionQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[] {
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Chunk>(),
                ComponentType.ReadOnly<Ungenerated>(),
                ComponentType.ReadOnly<BlockIDBuffer>()
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
        perm = new NativeArray<byte>(SimplexNoise.perm, Allocator.Persistent);
        system = World.Active.GetOrCreateSystem<DelayedChunkTaggingBufferSystem>();
    }

    protected override void OnDestroy()
    {
        perm.Dispose();
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
            RenderDistance = renderDistance,
            CommandBuffer = system.CreateCommandBuffer().ToConcurrent(),
            fillsThisFrame = 0,
            perm = perm

        };
        var job2 = new PositionChunksJob
        {
            blocks = GetBufferFromEntity<BlockIDBuffer>(true),
            PlayerPos = playerPosition,
            RenderDistance = renderDistance,
            ChunkEntities = Main.chunkEntities.AsParallelWriter()
        };
        //Apparently you should basically never use complete, and just pass the previous job
        //handle in as a dependency of the current job I believe

        var h1 = job2.Schedule<PositionChunksJob>(positionQuery, inputDeps);

        var h2 = job.Schedule<InitializeChunkBufferJob>(fillQuery, h1);
        system.AddJobHandleForProducer(h2);

        return JobHandle.CombineDependencies(h1, h2);
    }



    //Takes the player's current position and converts into chunk coordinates by dividing by chunk size
    //then it loads chunks around the player in a renderdistance radius
    //Move Chunk from requirecomponenttag to IJobForEachWithEntity<Translation> if ever needed to access

    //NOTE: Cannot use burstcompile with entitycommandbuffer 7/18/2019
    //NOTE: If I add teleportation, then this needs to be adjusted
    //I think I can use these instead of queries?
    //[RequireComponentTag(typeof(Chunk))]
    unsafe struct InitializeChunkBufferJob : IJobForEachWithEntity<Translation, Chunk, Ungenerated>
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<BlockIDBuffer> blocks;
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public int RenderDistance;
        public int fillsThisFrame;

        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<byte> perm;

        //Settings.RenderDistance * 2 * Settings.WorldHeight
        //x = mod width
        public EntityCommandBuffer.Concurrent CommandBuffer;
        //Proof of concept being done with renderdistance = 4
        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref Chunk chunk, [ReadOnly] ref Ungenerated ungenerated)
        {
            ushort x, y, z;
            int height;

            //First init
            if(blocks[entity].Length == 0)
            {
                for(ushort i = 0; i < 16 * 16 * 16; i++)
                {
                    //x = i & 15;
                    //y = i >> 8;
                    //z = (i >> 4) & 15;
                    MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
                    //1/128 = 0.0078125‬f
                    height = (32 + (int) (noise((translation.Value.x + x) * 0.0078125f, (translation.Value.z + z) * 0.0078125f) * 16.0f));
                    blocks[entity].Add(((chunk.pos.y * 16 + y) < height ? (ushort) 0 : (ushort) (2000)));
                }
                CommandBuffer.RemoveComponent<Ungenerated>(index, entity);
            }
            else
            {
                blocks[entity].Clear();

                for(ushort i = 0; i < 16 * 16 * 16; i++)
                {
                    //x = i & 15;
                    //y = i >> 8;
                    //z = (i >> 4) & 15;
                    MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
                    height = (32 + (int) (noise((translation.Value.x + x) * 0.0078125f, (translation.Value.z + z) * 0.0078125f) * 16.0f));
                    blocks[entity].Add(((translation.Value.y + y) < height ? (ushort) 0 : (ushort) (2000)));
                }
                CommandBuffer.RemoveComponent<Ungenerated>(index, entity);
            }
        }

        //This hopefully will get faster once entity commandbuffers can be burst compiled, then all this code can be too.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int fastfloor(float fp)
        {
            int i = (int) (fp);
            return (fp < i) ? (i - 1) : (i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte hash(int i)
        {
            return perm[(byte) (i)];
        }


        float grad(int hash, float x, float y)
        {
            int h = hash & 0x3F;  // Convert low 3 bits of hash code
            float u = h < 4 ? x : y;  // into 8 simple gradient directions,
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? -u : u) + ((h & 2) == 0 ? -2.0f * v : 2.0f * v); // and compute the dot product with (x,y).
        }

        float noise(float x, float y)
        {
            float n0, n1, n2;   // Noise contributions from the three corners

            // Skewing/Unskewing factors for 2D
            const float F2 = 0.366025403f;  // F2 = (sqrt(3) - 1) / 2
            const float G2 = 0.211324865f;  // G2 = (3 - sqrt(3)) / 6   = F2 / (1 + 2 * K)

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y) * F2;  // Hairy factor for 2D
            float xs = x + s;
            float ys = y + s;
            int i = fastfloor(xs);
            int j = fastfloor(ys);

            // Unskew the cell origin back to (x,y) space
            float t = (float) (i + j) * G2;
            float X0 = i - t;
            float Y0 = j - t;
            float x0 = x - X0;  // The x,y distances from the cell origin
            float y0 = y - Y0;

            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1;  // Offsets for second (middle) corner of simplex in (i,j) coords
            if(x0 > y0)
            {   // lower triangle, XY order: (0,0)->(1,0)->(1,1)
                i1 = 1;
                j1 = 0;
            }
            else
            {   // upper triangle, YX order: (0,0)->(0,1)->(1,1)
                i1 = 0;
                j1 = 1;
            }

            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6

            float x1 = x0 - i1 + G2;            // Offsets for middle corner in (x,y) unskewed coords
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1.0f + 2.0f * G2;   // Offsets for last corner in (x,y) unskewed coords
            float y2 = y0 - 1.0f + 2.0f * G2;

            // Work out the hashed gradient indices of the three simplex corners
            int gi0 = hash(i + hash(j));
            int gi1 = hash(i + i1 + hash(j + j1));
            int gi2 = hash(i + 1 + hash(j + 1));

            // Calculate the contribution from the first corner
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if(t0 < 0.0f)
            {
                n0 = 0.0f;
            }
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * grad(gi0, x0, y0);
            }

            // Calculate the contribution from the second corner
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if(t1 < 0.0f)
            {
                n1 = 0.0f;
            }
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * grad(gi1, x1, y1);
            }

            // Calculate the contribution from the third corner
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if(t2 < 0.0f)
            {
                n2 = 0.0f;
            }
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * grad(gi2, x2, y2);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return 45.23065f * (n0 + n1 + n2);
        }
    }

    unsafe struct PositionChunksJob : IJobForEachWithEntity<Translation, Ungenerated, Chunk>
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<BlockIDBuffer> blocks;
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public int RenderDistance;

        [NativeDisableParallelForRestriction] public NativeHashMap<int3, Entity>.ParallelWriter ChunkEntities;

        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref Ungenerated ungenerated, ref Chunk chunk)
        {
            //First init
            if(blocks[entity].Length == 0)
            {
                //Translation chunkPos = new Translation();
                //x increments every index but wraps every renderdistance*2 blocks
                //Offset by render distance so the player is in the middle though
                chunk.pos = new int3
                {
                    x = ((int) PlayerPos.x + index % (RenderDistance * 2)) - RenderDistance,
                    //y increments everytime an x-z layer is made (renderdistance*2)^2 or 2^6 and does not wrap
                    y = index / ((RenderDistance * 2) * (RenderDistance * 2)),
                    //z increments every time x completes a row, so every 8 blocks (renderdistance*2, which is 2^3)
                    //z wraps every renderdistance*2 blocks
                    z = ((int) PlayerPos.z + (index / (RenderDistance * 2)) % (RenderDistance * 2)) - RenderDistance
                };

                //Chunk pos is the position of a chunk in the world determined by the index in the job
                ChunkEntities.TryAdd(chunk.pos, entity);
                //multiply by size of chunk to get location in units
                translation.Value = new float3(chunk.pos.x * 16, chunk.pos.y * 16, chunk.pos.z * 16);
            }
            else
            {
                chunk.pos += ungenerated.offset;
                //Debug.Log(chunk.pos);
                translation.Value = chunk.pos * 16;

                ChunkEntities.TryAdd(chunk.pos, entity);
            }
        }
    }
}


public class DelayedChunkTaggingBufferSystem : EntityCommandBufferSystem
{

}