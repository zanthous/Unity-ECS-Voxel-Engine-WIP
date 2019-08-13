//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;
//using Unity.Mathematics;
//using UnityEngine;

////[DisableAutoCreation]
//public class ChunkMeshGenerationSystem : JobComponentSystem
//{
//    EntityQuery query;

//    //max size of 4096 triangles
//    //would happen if blocks were checkered in a chunk

//    DelayedChunkMeshTaggingBufferSystem system;

//    protected override void OnCreateManager()
//    {
//        query = GetEntityQuery(new EntityQueryDesc()
//        {
//            All = new ComponentType[]
//            {
//                ComponentType.ReadOnly<Chunk>(),
//                ComponentType.ReadOnly<BlockIDBuffer>(),
//                ComponentType.ReadWrite<Triangle>(),
//                ComponentType.ReadWrite<Normal>(),
//                ComponentType.ReadWrite<Vertex>(),
//                ComponentType.ReadWrite<Uv>()
//            },
//            Any = System.Array.Empty<ComponentType>(),
//            //Chunk must be generated to have its mesh updated
//            None = new ComponentType[]
//            {
//                ComponentType.ReadOnly<MeshDirty>(),
//                ComponentType.ReadOnly<Ungenerated>(),
//                ComponentType.ReadOnly<ChunkUpToDate>()
//            }
//        });
//        //This shit doesn't work at all actually
//        //query.SetFilterChanged(ComponentType.ReadOnly<BlockIDBuffer>());
//        system = World.Active.GetOrCreateSystem<DelayedChunkMeshTaggingBufferSystem>();
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        var job = new GenerateMeshJob
//        {
//            Vertices = GetBufferFromEntity<Vertex>(false),
//            Normals = GetBufferFromEntity<Normal>(false),
//            Tris = GetBufferFromEntity<Triangle>(false),
//            Uvs = GetBufferFromEntity<Uv>(false),
//            blocks = GetBufferFromEntity<BlockIDBuffer>(true),
//            CommandBuffer = system.CreateCommandBuffer().ToConcurrent(),
//            EntityType = GetArchetypeChunkEntityType(),
//            meshesThisFrame = 0
//        };
//        inputDeps = job.Schedule<GenerateMeshJob>(query, inputDeps);
//        system.AddJobHandleForProducer(inputDeps);
//        return inputDeps;
//    }

//    protected override void OnDestroy()
//    {
//    }

//    public class DelayedChunkMeshTaggingBufferSystem : EntityCommandBufferSystem { }
//    //[BurstCompile]
//    unsafe struct GenerateMeshJob : IJobChunk
//    {
//        //https://forum.unity.com/threads/is-it-better-to-use-ijobparallelfor-for-larger-or-smaller-processes.576856/#post-3842758
//        //https://forum.unity.com/threads/nativearray-and-mesh.522951/#post-3839548x    
//        [NativeDisableParallelForRestriction] public BufferFromEntity<Vertex> Vertices;
//        [NativeDisableParallelForRestriction] public BufferFromEntity<Triangle> Tris;
//        [NativeDisableParallelForRestriction] public BufferFromEntity<Uv> Uvs;
//        [NativeDisableParallelForRestriction] public BufferFromEntity<Normal> Normals;
//        [ReadOnly] public ArchetypeChunkEntityType EntityType;

//        [NativeDisableParallelForRestriction] [ReadOnly] public BufferFromEntity<BlockIDBuffer> blocks;

//        public EntityCommandBuffer.Concurrent CommandBuffer;
//        public int meshesThisFrame;

//        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//        {
//            //Testing updating meshes slowly
//            if(meshesThisFrame > 8)
//                return;

//            meshesThisFrame++;

//            var entities = chunk.GetNativeArray(EntityType);
//            var entity = entities[0];
//            if(Vertices[entity].Length > 0)
//            {
//                //Debug.Log("had to clear");
//                Vertices[entity].Clear();
//                Tris[entity].Clear();
//                Uvs[entity].Clear();
//                Normals[entity].Clear();
//            }

//            //TODO try without any variables and compare

//            int xOff = 1;
//            int yOff = 256;
//            int zOff = 16;

//            int vertIndex = 0;

//            ushort x, y, z;
//            for(ushort i = 0; i < 4096; i++)
//            {
//                if(blocks[entity][i] > 1999)
//                    continue;
//                //x = i & 15;
//                //y = i >> 8;
//                //z = (i >> 4) & 15;
//                MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
//                //Solid cubical blocks are all under 2000
//                //since i likely cant access static data about block types
//                //some sort of inherent organization may be necessary for now
//                //left
//                //tris 6 others 4
//                //unity is cw for front facing triangles
//                if(x == 0 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16((ushort) (x - 1), y, z)].Value > 1999)
//                {
//                    Normals[entity].Add(new float3(-1, 0, 0));
//                    Normals[entity].Add(new float3(-1, 0, 0));
//                    Normals[entity].Add(new float3(-1, 0, 0));
//                    Normals[entity].Add(new float3(-1, 0, 0));

//                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
//                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
//                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
//                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));

//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 3);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 1);

//                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//                    vertIndex += 4;
//                }
//                //right
//                if(x == 15 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16((ushort) (x + 1), y, z)].Value > 1999)
//                {
//                    Normals[entity].Add(new float3(1, 0, 0));
//                    Normals[entity].Add(new float3(1, 0, 0));
//                    Normals[entity].Add(new float3(1, 0, 0));
//                    Normals[entity].Add(new float3(1, 0, 0));

//                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));
//                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
//                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));

//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 3);

//                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//                    vertIndex += 4;
//                }
//                //back
//                if(z == 0 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, y, (ushort) (z - 1))].Value > 1999)
//                {
//                    Normals[entity].Add(new float3(0, 0, -1));
//                    Normals[entity].Add(new float3(0, 0, -1));
//                    Normals[entity].Add(new float3(0, 0, -1));
//                    Normals[entity].Add(new float3(0, 0, -1));

//                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
//                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
//                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
//                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 3);

//                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//                    vertIndex += 4;
//                }
//                //front
//                if(z == 15 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, y, (ushort) (z + 1))].Value > 1999)
//                {
//                    Normals[entity].Add(new float3(0, 0, 1));
//                    Normals[entity].Add(new float3(0, 0, 1));
//                    Normals[entity].Add(new float3(0, 0, 1));
//                    Normals[entity].Add(new float3(0, 0, 1));

//                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
//                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));

//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 3);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 0);

//                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//                    vertIndex += 4;
//                }
//                //below
//                if(y == 0 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, (ushort) (y - 1), z)].Value > 1999)
//                {
//                    Normals[entity].Add(new float3(0, -1, 0));
//                    Normals[entity].Add(new float3(0, -1, 0));
//                    Normals[entity].Add(new float3(0, -1, 0));
//                    Normals[entity].Add(new float3(0, -1, 0));

//                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
//                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 3);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 0);

//                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//                    vertIndex += 4;
//                }
//                //above
//                if(y == 15 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, (ushort) (y + 1), z)].Value > 1999)
//                {
//                    Normals[entity].Add(new float3(0, 1, 0));
//                    Normals[entity].Add(new float3(0, 1, 0));
//                    Normals[entity].Add(new float3(0, 1, 0));
//                    Normals[entity].Add(new float3(0, 1, 0));

//                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
//                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
//                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));

//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 1);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 0);
//                    Tris[entity].Add(vertIndex + 2);
//                    Tris[entity].Add(vertIndex + 3);

//                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//                    vertIndex += 4;
//                }
//            }
//            //Tests suggest this is slower
//            #region nested_loops
//            //int i;
//            //for(int y = 0; y < 16; y++)
//            //{
//            //    for(int z = 0; z < 16; z++)
//            //    {
//            //        for(int x = 0; x < 16; x++)
//            //        {
//            //            i = (z << 4) + (y << 8) + x;
//            //            if(blocks[entity][i] > 1999)
//            //                continue;
//            //            Solid cubical blocks are all under 2000
//            //            since i likely cant access static data about block types
//            //            some sort of inherent organization may be necessary for now
//            //            left
//            //            tris 6 others 4
//            //            unity is cw for front facing triangles
//            //            if(x == 0 || blocks[entity][i - xOff].Value > 1999)
//            //                    {
//            //                        Normals[entity].Add(new float3(-1, 0, 0));
//            //                        Normals[entity].Add(new float3(-1, 0, 0));
//            //                        Normals[entity].Add(new float3(-1, 0, 0));
//            //                        Normals[entity].Add(new float3(-1, 0, 0));

//            //                        Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
//            //                        Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
//            //                        Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
//            //                        Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));

//            //                        Tris[entity].Add(vertIndex + 0);
//            //                        Tris[entity].Add(vertIndex + 1);
//            //                        Tris[entity].Add(vertIndex + 2);
//            //                        Tris[entity].Add(vertIndex + 3);
//            //                        Tris[entity].Add(vertIndex + 2);
//            //                        Tris[entity].Add(vertIndex + 1);

//            //                        Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//            //                        Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//            //                        Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//            //                        Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//            //                        vertIndex += 4;
//            //                    }
//            //            right
//            //            if(x == 15 || blocks[entity][i + xOff].Value > 1999)
//            //            {
//            //                Normals[entity].Add(new float3(1, 0, 0));
//            //                Normals[entity].Add(new float3(1, 0, 0));
//            //                Normals[entity].Add(new float3(1, 0, 0));
//            //                Normals[entity].Add(new float3(1, 0, 0));

//            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));
//            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
//            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));

//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 1);
//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 1);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 3);

//            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//            //                vertIndex += 4;
//            //            }
//            //            back
//            //            if(z == 0 || blocks[entity][i - zOff].Value > 1999)
//            //            {
//            //                Normals[entity].Add(new float3(0, 0, -1));
//            //                Normals[entity].Add(new float3(0, 0, -1));
//            //                Normals[entity].Add(new float3(0, 0, -1));
//            //                Normals[entity].Add(new float3(0, 0, -1));

//            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
//            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
//            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
//            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 1);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 3);

//            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//            //                vertIndex += 4;
//            //            }
//            //            front
//            //            if(z == 15 || blocks[entity][i + zOff].Value > 1999)
//            //            {
//            //                Normals[entity].Add(new float3(0, 0, 1));
//            //                Normals[entity].Add(new float3(0, 0, 1));
//            //                Normals[entity].Add(new float3(0, 0, 1));
//            //                Normals[entity].Add(new float3(0, 0, 1));

//            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
//            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));

//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 1);
//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 3);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 0);

//            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//            //                vertIndex += 4;
//            //            }
//            //            below
//            //            if(y == 0 || blocks[entity][i - yOff].Value > 1999)
//            //            {
//            //                Normals[entity].Add(new float3(0, -1, 0));
//            //                Normals[entity].Add(new float3(0, -1, 0));
//            //                Normals[entity].Add(new float3(0, -1, 0));
//            //                Normals[entity].Add(new float3(0, -1, 0));

//            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
//            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 1);
//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 3);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 0);

//            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//            //                vertIndex += 4;
//            //            }
//            //            above
//            //            if(y == 15 || blocks[entity][i + yOff].Value > 1999)
//            //            {
//            //                Normals[entity].Add(new float3(0, 1, 0));
//            //                Normals[entity].Add(new float3(0, 1, 0));
//            //                Normals[entity].Add(new float3(0, 1, 0));
//            //                Normals[entity].Add(new float3(0, 1, 0));

//            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
//            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
//            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));

//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 1);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 0);
//            //                Tris[entity].Add(vertIndex + 2);
//            //                Tris[entity].Add(vertIndex + 3);

//            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
//            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
//            //                vertIndex += 4;
//            //            }
//            //        }
//            //    }
//            //}
//            #endregion
//            //meshDirty_in.Dirty = true;
//            // = new MeshDirty { Entity = entity, Dirty = true };
//            CommandBuffer.AddComponent<MeshDirty>(chunkIndex, entity, new MeshDirty { Entity = entity });
//        }
//    }
//}

//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Transforms;
//using Unity.Burst;
//using System;
//using Unity.Mathematics;
//using UnityEngine;

////ChunkManager?

//public class ChunkBlockIDFill : JobComponentSystem
//{
//    EntityQuery query;
//    EntityQuery playerQuery;
//    //private int renderX = Settings.RenderDistance * 2;
//    //private int renderY = Settings.WorldHeight / Settings.ChunkSize;
//    //private int renderZ = Settings.RenderDistance * 2;

//    Player playerRef;

//    //I don't know what thing to use here so I took a random one
//    //What I should use may be written here somewhere:
//    //https://docs.unity3d.com/Packages/com.unity.entities@0.0/api/Unity.Entities.EntityCommandBufferSystem.html

//    //Component system update ordering is now hierarchical. A forthcoming document will cover this feature in detail. Key changes:
//    //Added ComponentSystemGroup class, representing a group of systems (and system groups) to update in a fixed order.
//    //The following ComponentSystemGroups are added to the Unity player loop by default:
//    //InitializationSystemGroup (in the Initialization phase)
//    //SimulationSystemGroup (in the FixedUpdate phase)
//    //PresentationSystemGroup (in the Update phase)
//    //..etc taken from:
//    //https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/ReleaseNotes.md#changes-6
//    //From my best guess from the text above this would be what I use
//    //DelayedChunkTaggingBufferSystem system;
//    //Using this had no apparent effect so I will return to the other version I had
//    //DelayedChunkTaggingBufferSystem system;
//    //Actually trying one more time while adding a updateingroup tag to this system
//    //No noticeable effect
//    //Temporarily just returning from the execute function if accidentally called into again
//    //Definitely need to fix this later somehow


//    DelayedChunkTaggingBufferSystem system;

//    protected override void OnCreateManager()
//    {
//        query = GetEntityQuery(new EntityQueryDesc()
//        {
//            All = new ComponentType[] {
//                ComponentType.ReadWrite<Translation>(),
//                ComponentType.ReadOnly<Chunk>(),
//                ComponentType.ReadOnly<Ungenerated>(),
//                ComponentType.ReadWrite<BlockIDBuffer>()
//            },
//            Any = System.Array.Empty<ComponentType>(),
//            None = System.Array.Empty<ComponentType>()
//        });
//        playerQuery = GetEntityQuery(new EntityQueryDesc()
//        {
//            All = new ComponentType[]
//            {
//                ComponentType.ReadOnly<Player>(),
//                ComponentType.ReadOnly<Translation>()
//            }
//        });

//        system = World.Active.GetOrCreateSystem<DelayedChunkTaggingBufferSystem>();
//    }

//    protected override void OnDestroyManager()
//    {
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        //TODO
//        var players = playerQuery.ToComponentDataArray<Player>(Allocator.TempJob);
//        playerRef = players[0];
//        float3 playerPosition = playerRef.position;
//        int renderDistance = playerRef.renderDistance;
//        players.Dispose();

//        //var player = get
//        //var pos = GetComponentDataFromEntity<Player>();

//        //if(playerRef.Length>1)
//        //{
//        //    Debug.Log("More than 1 player found! - ChunkIterationTest.cs");
//        //}
//        //else if(playerRef.Length == 0)
//        //{
//        //    Debug.Log("No player found! - ChunkIterationTest.cs");
//        //}

//        //AddJobHandleForProducer
//        var job = new InitializeChunkBufferJob
//        {
//            blocks = GetBufferFromEntity<BlockIDBuffer>(false),
//            PlayerPos = playerPosition,
//            RenderDistance = playerRef.renderDistance,
//            CommandBuffer = system.CreateCommandBuffer().ToConcurrent()
//        };
//        inputDeps = job.Schedule<InitializeChunkBufferJob>(query, inputDeps);
//        system.AddJobHandleForProducer(inputDeps);
//        return inputDeps;
//    }



//    //Takes the player's current position and converts into chunk coordinates by dividing by chunk size
//    //then it loads chunks around the player in a renderdistance radius
//    //Move Chunk from requirecomponenttag to IJobForEachWithEntity<Translation> if ever needed to access

//    //NOTE: Cannot use burstcompile with entitycommandbuffer 7/18/2019
//    //NOTE: If I add teleportation, then this needs to be adjusted
//    //I think I can use these instead of queries?
//    //[RequireComponentTag(, typeof(BlockIDBuffer), typeof(Chunk))]
//    unsafe struct InitializeChunkBufferJob : IJobForEachWithEntity<Translation, Ungenerated>
//    {
//        [NativeDisableParallelForRestriction] public BufferFromEntity<BlockIDBuffer> blocks;
//        [ReadOnly] public float3 PlayerPos;
//        [ReadOnly] public int RenderDistance;
//        //Settings.RenderDistance * 2 * Settings.WorldHeight
//        //x = mod width
//        public EntityCommandBuffer.Concurrent CommandBuffer;
//        //Proof of concept being done with renderdistance = 4
//        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref Ungenerated ungenerated)
//        {
//            ////Running into an issue currently where the entitycommandbuffer isn't properly removing
//            ////generated chunks out of this job 
//            //if(blocks[entity].Length !=0)
//            //{
//            //    Debug.Log("Error! Chunk already initialized!");
//            //    return;
//            //}

//            //First init
//            if(blocks[entity].Length == 0)
//            {
//                Translation chunkPos = new Translation();
//                //x increments every index but wraps every renderdistance*2 blocks
//                //Offset by render distance so the player is in the middle though
//                chunkPos.Value.x = ((int) PlayerPos.x + index % (RenderDistance * 2)) - RenderDistance;
//                //y increments everytime an x-z layer is made (renderdistance*2)^2 or 2^6 and does not wrap
//                chunkPos.Value.y = index / ((RenderDistance * 2) * (RenderDistance * 2)); //
//                                                                                          //z increments every time x completes a row, so every 8 blocks (renderdistance*2, which is 2^3)
//                                                                                          //z wraps every renderdistance*2 blocks
//                chunkPos.Value.z = ((int) PlayerPos.z + (index / (RenderDistance * 2)) % (RenderDistance * 2)) - RenderDistance;
//                //Chunk pos is the position of a chunk in the world determined by the index in the job

//                //multiply by size of chunk to get location in units
//                translation.Value = chunkPos.Value * 16;

//                //blocks[entity].Reserve(16 * 16 * 16);

//                ushort x, y, z;
//                int height;

//                ushort test;
//                for(ushort i = 0; i < 16 * 16 * 16; i++)
//                {
//                    //x = i & 15;
//                    //y = i >> 8;
//                    //z = (i >> 4) & 15;
//                    MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
//                    height = (32 + (int) (Mathf.PerlinNoise((translation.Value.x + x) * 0.015625f, (translation.Value.z + z) * 0.015625f) * 70.0f));
//                    blocks[entity].Add(((chunkPos.Value.y * 16 + y) < height ? (ushort) 0 : (ushort) (2000)));
//                }
//                CommandBuffer.RemoveComponent<Ungenerated>(index, entity);
//            }
//            else
//            {
//                //TODO 
//                blocks[entity].Clear();

//                //move over renderdistance+1
//                if(ungenerated.xtruezfalse)
//                {
//                    if(translation.Value.x > PlayerPos.x)
//                    {
//                        translation.Value.x -= (RenderDistance * 2) * 16;
//                        //y unchanged
//                        //z unchanged
//                    }
//                    else
//                    {
//                        translation.Value.x += (RenderDistance * 2) * 16;
//                        //y unchanged
//                        //z unchanged
//                    }
//                }
//                else
//                {
//                    if(translation.Value.z > PlayerPos.z)
//                    {
//                        translation.Value.z -= (RenderDistance * 2) * 16;
//                    }
//                    else
//                    {
//                        translation.Value.z += (RenderDistance * 2) * 16;
//                    }

//                    //Debug.Log("I fucked up");
//                    //Debug.Log("Trans x: " + translation.Value.x + "\n Player chunkX scaled up: " + (ChunkPos.x * 16));
//                    //Debug.Log("Trans z: " + translation.Value.x + "\n Player chunkZ scaled up: " + (ChunkPos.z * 16));
//                    //Debug.Log("x result: " + Mathf.Abs(translation.Value.x - (PlayerPos.x * 16.0f)));
//                    //Debug.Log("z result: " + Mathf.Abs(translation.Value.z - (PlayerPos.z * 16.0f)));
//                }
//                //translation.Value = chunkPos.Value * 16;

//                //blocks[entity].Reserve(16 * 16 * 16);

//                ushort x, y, z;
//                int height;

//                for(ushort i = 0; i < 16 * 16 * 16; i++)
//                {
//                    //x = i & 15;
//                    //y = i >> 8;
//                    //z = (i >> 4) & 15;
//                    MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
//                    height = (32 + (int) (Mathf.PerlinNoise((translation.Value.x + x) * 0.015625f, (translation.Value.z + z) * 0.015625f) * 70.0f));
//                    blocks[entity].Add(((translation.Value.y + y) < height ? (ushort) 0 : (ushort) (2000)));
//                }
//                CommandBuffer.RemoveComponent<Ungenerated>(index, entity);
//            }
//        }
//    }
//}


//public class DelayedChunkTaggingBufferSystem : EntityCommandBufferSystem
//{

//}