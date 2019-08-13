using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

//[DisableAutoCreation]
public class ChunkMeshGenerationSystem : JobComponentSystem
{
    EntityQuery query;

    //max size of 4096 triangles
    //would happen if blocks were checkered in a chunk

    DelayedChunkMeshTaggingBufferSystem system;

    protected override void OnCreateManager()
    {
        query = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Chunk>(),
                ComponentType.ReadOnly<BlockIDBuffer>(),
                ComponentType.ReadWrite<Triangle>(),
                ComponentType.ReadWrite<Normal>(),
                ComponentType.ReadWrite<Vertex>(),
                ComponentType.ReadWrite<Uv>()
            },
            Any = System.Array.Empty<ComponentType>(),
            //Chunk must be generated to have its mesh updated
            None = new ComponentType[]
            {
                ComponentType.ReadOnly<MeshDirty>(),
                ComponentType.ReadOnly<Ungenerated>(),
                ComponentType.ReadOnly<ChunkUpToDate>()
            }
        });
        //This shit doesn't work at all actually
        //query.SetFilterChanged(ComponentType.ReadOnly<BlockIDBuffer>());
        system = World.Active.GetOrCreateSystem<DelayedChunkMeshTaggingBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new GenerateMeshJob
        {
            Vertices = GetBufferFromEntity<Vertex>(false),
            Normals = GetBufferFromEntity<Normal>(false),
            Tris = GetBufferFromEntity<Triangle>(false),
            Uvs = GetBufferFromEntity<Uv>(false),
            blocks = GetBufferFromEntity<BlockIDBuffer>(true),
            CommandBuffer = system.CreateCommandBuffer().ToConcurrent(),
            EntityType = GetArchetypeChunkEntityType(),
            meshesThisFrame = 0
        };
        inputDeps = job.Schedule<GenerateMeshJob>(query, inputDeps);
        system.AddJobHandleForProducer(inputDeps);
        return inputDeps;
    }

    protected override void OnDestroy()
    {
    }

    public class DelayedChunkMeshTaggingBufferSystem : EntityCommandBufferSystem { }
    //[BurstCompile]
    unsafe struct GenerateMeshJob : IJobChunk
    {
        //https://forum.unity.com/threads/is-it-better-to-use-ijobparallelfor-for-larger-or-smaller-processes.576856/#post-3842758
        //https://forum.unity.com/threads/nativearray-and-mesh.522951/#post-3839548x    
        [NativeDisableParallelForRestriction] public BufferFromEntity<Vertex> Vertices;
        [NativeDisableParallelForRestriction] public BufferFromEntity<Triangle> Tris;
        [NativeDisableParallelForRestriction] public BufferFromEntity<Uv> Uvs;
        [NativeDisableParallelForRestriction] public BufferFromEntity<Normal> Normals;
        [ReadOnly] public ArchetypeChunkEntityType EntityType;

        [NativeDisableParallelForRestriction] [ReadOnly] public BufferFromEntity<BlockIDBuffer> blocks;

        public EntityCommandBuffer.Concurrent CommandBuffer;
        public int meshesThisFrame;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            //Testing updating meshes slowly
            if(meshesThisFrame > 8)
                return;

            meshesThisFrame++;

            var entities = chunk.GetNativeArray(EntityType);
            var entity = entities[0];
            if(Vertices[entity].Length > 0)
            {
                //Debug.Log("had to clear");
                Vertices[entity].Clear();
                Tris[entity].Clear();
                Uvs[entity].Clear();
                Normals[entity].Clear();
            }

            //TODO try without any variables and compare

            int xOff = 1;
            int yOff = 256;
            int zOff = 16;

            int vertIndex = 0;

            ushort x, y, z;
            for(ushort i = 0; i < 4096; i++)
            {
                if(blocks[entity][i] > 1999)
                    continue;
                //x = i & 15;
                //y = i >> 8;
                //z = (i >> 4) & 15;
                MortonUtility.m3d_d_sLUT16(i, &x, &y, &z);
                //Solid cubical blocks are all under 2000
                //since i likely cant access static data about block types
                //some sort of inherent organization may be necessary for now
                //left
                //tris 6 others 4
                //unity is cw for front facing triangles
                if(x == 0 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16((ushort) (x - 1), y, z)].Value > 1999)
                {
                    Normals[entity].Add(new float3(-1, 0, 0));
                    Normals[entity].Add(new float3(-1, 0, 0));
                    Normals[entity].Add(new float3(-1, 0, 0));
                    Normals[entity].Add(new float3(-1, 0, 0));

                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));

                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 3);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 1);

                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
                    vertIndex += 4;
                }
                //right
                if(x == 15 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16((ushort) (x + 1), y, z)].Value > 1999)
                {
                    Normals[entity].Add(new float3(1, 0, 0));
                    Normals[entity].Add(new float3(1, 0, 0));
                    Normals[entity].Add(new float3(1, 0, 0));
                    Normals[entity].Add(new float3(1, 0, 0));

                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));
                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));

                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 3);

                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
                    vertIndex += 4;
                }
                //back
                if(z == 0 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, y, (ushort) (z - 1))].Value > 1999)
                {
                    Normals[entity].Add(new float3(0, 0, -1));
                    Normals[entity].Add(new float3(0, 0, -1));
                    Normals[entity].Add(new float3(0, 0, -1));
                    Normals[entity].Add(new float3(0, 0, -1));

                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 3);

                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
                    vertIndex += 4;
                }
                //front
                if(z == 15 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, y, (ushort) (z + 1))].Value > 1999)
                {
                    Normals[entity].Add(new float3(0, 0, 1));
                    Normals[entity].Add(new float3(0, 0, 1));
                    Normals[entity].Add(new float3(0, 0, 1));
                    Normals[entity].Add(new float3(0, 0, 1));

                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));

                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 3);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 0);

                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
                    vertIndex += 4;
                }
                //below
                if(y == 0 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, (ushort) (y - 1), z)].Value > 1999)
                {
                    Normals[entity].Add(new float3(0, -1, 0));
                    Normals[entity].Add(new float3(0, -1, 0));
                    Normals[entity].Add(new float3(0, -1, 0));
                    Normals[entity].Add(new float3(0, -1, 0));

                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
                    Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 3);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 0);

                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
                    vertIndex += 4;
                }
                //above
                if(y == 15 || blocks[entity][(int) MortonUtility.m3d_e_sLUT16(x, (ushort) (y + 1), z)].Value > 1999)
                {
                    Normals[entity].Add(new float3(0, 1, 0));
                    Normals[entity].Add(new float3(0, 1, 0));
                    Normals[entity].Add(new float3(0, 1, 0));
                    Normals[entity].Add(new float3(0, 1, 0));

                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
                    Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
                    Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));

                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 1);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 0);
                    Tris[entity].Add(vertIndex + 2);
                    Tris[entity].Add(vertIndex + 3);

                    Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
                    Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
                    Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
                    vertIndex += 4;
                }
            }
            //Tests suggest this is slower
            #region nested_loops
            //int i;
            //for(int y = 0; y < 16; y++)
            //{
            //    for(int z = 0; z < 16; z++)
            //    {
            //        for(int x = 0; x < 16; x++)
            //        {
            //            i = (z << 4) + (y << 8) + x;
            //            if(blocks[entity][i] > 1999)
            //                continue;
            //            Solid cubical blocks are all under 2000
            //            since i likely cant access static data about block types
            //            some sort of inherent organization may be necessary for now
            //            left
            //            tris 6 others 4
            //            unity is cw for front facing triangles
            //            if(x == 0 || blocks[entity][i - xOff].Value > 1999)
            //                    {
            //                        Normals[entity].Add(new float3(-1, 0, 0));
            //                        Normals[entity].Add(new float3(-1, 0, 0));
            //                        Normals[entity].Add(new float3(-1, 0, 0));
            //                        Normals[entity].Add(new float3(-1, 0, 0));

            //                        Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
            //                        Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
            //                        Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
            //                        Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));

            //                        Tris[entity].Add(vertIndex + 0);
            //                        Tris[entity].Add(vertIndex + 1);
            //                        Tris[entity].Add(vertIndex + 2);
            //                        Tris[entity].Add(vertIndex + 3);
            //                        Tris[entity].Add(vertIndex + 2);
            //                        Tris[entity].Add(vertIndex + 1);

            //                        Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
            //                        Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
            //                        Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
            //                        Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
            //                        vertIndex += 4;
            //                    }
            //            right
            //            if(x == 15 || blocks[entity][i + xOff].Value > 1999)
            //            {
            //                Normals[entity].Add(new float3(1, 0, 0));
            //                Normals[entity].Add(new float3(1, 0, 0));
            //                Normals[entity].Add(new float3(1, 0, 0));
            //                Normals[entity].Add(new float3(1, 0, 0));

            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));
            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));

            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 1);
            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 1);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 3);

            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
            //                vertIndex += 4;
            //            }
            //            back
            //            if(z == 0 || blocks[entity][i - zOff].Value > 1999)
            //            {
            //                Normals[entity].Add(new float3(0, 0, -1));
            //                Normals[entity].Add(new float3(0, 0, -1));
            //                Normals[entity].Add(new float3(0, 0, -1));
            //                Normals[entity].Add(new float3(0, 0, -1));

            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));
            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 1);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 3);

            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
            //                vertIndex += 4;
            //            }
            //            front
            //            if(z == 15 || blocks[entity][i + zOff].Value > 1999)
            //            {
            //                Normals[entity].Add(new float3(0, 0, 1));
            //                Normals[entity].Add(new float3(0, 0, 1));
            //                Normals[entity].Add(new float3(0, 0, 1));
            //                Normals[entity].Add(new float3(0, 0, 1));

            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));

            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 1);
            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 3);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 0);

            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
            //                vertIndex += 4;
            //            }
            //            below
            //            if(y == 0 || blocks[entity][i - yOff].Value > 1999)
            //            {
            //                Normals[entity].Add(new float3(0, -1, 0));
            //                Normals[entity].Add(new float3(0, -1, 0));
            //                Normals[entity].Add(new float3(0, -1, 0));
            //                Normals[entity].Add(new float3(0, -1, 0));

            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 0));
            //                Vertices[entity].Add(new float3(x + 0, y + 0, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 0, z + 0));

            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 1);
            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 3);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 0);

            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
            //                vertIndex += 4;
            //            }
            //            above
            //            if(y == 15 || blocks[entity][i + yOff].Value > 1999)
            //            {
            //                Normals[entity].Add(new float3(0, 1, 0));
            //                Normals[entity].Add(new float3(0, 1, 0));
            //                Normals[entity].Add(new float3(0, 1, 0));
            //                Normals[entity].Add(new float3(0, 1, 0));

            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 0));
            //                Vertices[entity].Add(new float3(x + 0, y + 1, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 1));
            //                Vertices[entity].Add(new float3(x + 1, y + 1, z + 0));

            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 1);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 0);
            //                Tris[entity].Add(vertIndex + 2);
            //                Tris[entity].Add(vertIndex + 3);

            //                Uvs[entity].Add(new float3(0, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 0, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(1, 1, blocks[entity][i]));
            //                Uvs[entity].Add(new float3(0, 1, blocks[entity][i]));
            //                vertIndex += 4;
            //            }
            //        }
            //    }
            //}
            #endregion
            //meshDirty_in.Dirty = true;
            // = new MeshDirty { Entity = entity, Dirty = true };
            CommandBuffer.AddComponent<MeshDirty>(chunkIndex, entity, new MeshDirty { Entity = entity });
        }
    }
}