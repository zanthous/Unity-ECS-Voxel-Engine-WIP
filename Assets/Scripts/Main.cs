using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class Main : MonoBehaviour
{
    //https://forum.unity.com/threads/best-practices-for-big-data-sets-arrays-on-entities.523642/
    //very important thread

    //notes from voxel example
    //uses a hash to give an id to every voxel (xxhash)

    //https://forum.unity.com/threads/lists-of-things-collections-arrays-groups.524532/
    //another thread

    //kept reading looked at sharedcomponentdata which seems bad, and fixed array which was replaced with
    //dynamic buffer which I will look at now-

    static private bool initDone = false;

    private ChunkBlockIDFill chunkDataGenerationSystem;
    private ChunkMeshGenerationSystem chunkMeshBufferFillSystem;
    private MeshSystem meshSetSystem;
    private readonly HashSet<Entity> entitySet = new HashSet<Entity>();
    private Material material;
    
    EntityManager em;

    static public bool InitDone { get => initDone; }

    //private InstancedRenderMeshBatchGroup instancedRenderMeshBatchGroup;

    void Awake()
    {
        material = Resources.Load<Material>("Materials/New Material");
        em = World.Active.EntityManager;

        var blockChunk = em.CreateArchetype(
            ComponentType.ReadWrite<Translation>(),
            //ComponentType.ReadWrite<LocalToWorld>(),
            //ComponentType.ReadWrite<RenderMesh>(),
            //ComponentType.ReadWrite<Scale>(),
            ComponentType.ReadWrite<Chunk>(),
            ComponentType.ReadOnly<Ungenerated>());

        var playerAT = em.CreateArchetype(
            ComponentType.ReadWrite<Translation>(),
            //ComponentType.ReadWrite<LocalToWorld>(),
            //ComponentType.ReadWrite<Scale>(),
            ComponentType.ReadWrite<Player>());

        var player = em.CreateEntity(playerAT);

        Player playerTest;
        playerTest.chunkX = 0;
        playerTest.chunkY = 0;
        playerTest.chunkZ = 0;
        playerTest.renderDistance = Settings.RenderDistance;
        playerTest.position = new float3();
        
        em.SetComponentData<Player>(player, playerTest);
        //em.SetComponentData<Scale>(player, new Scale { Value = 1 });
        Entity temp;
        for(int i = 0; i < (Settings.RenderDistance * 2) * (Settings.RenderDistance * 2) * (Settings.WorldHeight / Settings.ChunkSize); i++)
        {
            temp = em.CreateEntity(blockChunk);
            em.AddBuffer<BlockIDBuffer>(temp);
            em.AddBuffer<Triangle>(temp);
            em.AddBuffer<Vertex>(temp);
            em.AddBuffer<Normal>(temp);
            em.AddBuffer<Uv>(temp);
            //em.SetSharedComponentData<RenderMesh>(temp, new RenderMesh
            //{
            //    mesh = new Mesh(),
            //    material = Resources.Load<Material>("New Material"),
            //    castShadows = UnityEngine.Rendering.ShadowCastingMode.On,
            //    receiveShadows = true
            //});
            MeshSystem.meshes.Add(temp, new Mesh());

            //em.SetComponentData<Scale>(temp, new Scale { Value = 1 });
            //em.AddComponentObject(temp, MeshFilter is uncreatable?);
            entitySet.Add(temp);

        }

        chunkDataGenerationSystem = World.Active.GetOrCreateSystem<ChunkBlockIDFill>();
        chunkMeshBufferFillSystem = World.Active.GetOrCreateSystem<ChunkMeshGenerationSystem>();
        meshSetSystem = World.Active.GetOrCreateSystem<MeshSystem>();
        initDone = true;
        //World.Active.DestroySystem();
        World.Active.GetExistingSystem<PresentationSystemGroup>().Enabled = false;

        //
        //unsafe
        //{
        //    ushort i, j, k = 0;
        //    for(ushort z = 0; z < 16; z++)
        //    {
        //        for(ushort y = 0; y < 16; y++)
        //        {
        //            for(ushort x = 0; x < 16; x++)
        //            {
        //                uint code = MortonUtility.m3d_e_sLUT16(x, y, z);
        //                Debug.Log(code);
        //                MortonUtility.m3d_d_sLUT16(code, &i, &j, &k);
        //                Debug.Log("x: " + i + " y: " + j + " z: " + k);
        //            }
        //        }
        //    }
        //}
    }

    private void Update()
    {
        //Just getting the components takes 2.5 ms
        foreach(Entity e in entitySet)
        {
            //if(em.HasComponent<Ungenerated>(e))
            //    continue;
            //Graphics.DrawMesh(em.GetSharedComponentData<RenderMesh>(e).mesh, em.GetComponentData<Translation>(e).Value, Quaternion.identity, material, 0);

            if(!em.HasComponent<ChunkUpToDate>(e))
                continue;
            Graphics.DrawMesh(MeshSystem.meshes[e], em.GetComponentData<Translation>(e).Value, Quaternion.identity, material, 0);
            //if(MeshSystem.meshes.ContainsKey(e))
            //{
            
            //}

            //em.GetSharedComponentData<RenderMesh>(e);
            //em.GetComponentData<Translation>(e);
        }
        

        //chunkDataGenerationSystem.Update();
        //chunkMeshBufferFillSystem.Update();
        //meshSetSystem.Update();
        #region chunkgeneration_timing
        //if(!UseParallel)
        //{
        //    start = Time.realtimeSinceStartup;
        //    PopulateChunkJob populateChunkJob = new PopulateChunkJob
        //    {
        //        blocks = chunk
        //    };
        //    JobHandle jobHandle = populateChunkJob.Schedule();
        //    jobHandle.Complete();
        //    Debug.Log(((Time.realtimeSinceStartup - start) * 1000f) + "ms");
        //}
        //else
        //{
        //    start = Time.realtimeSinceStartup;
        //    PopulateChunkJobParallel populateChunkJobParallel = new PopulateChunkJobParallel
        //    {
        //        blocks = chunk
        //    };
        //    JobHandle jobHandle2 = populateChunkJobParallel.Schedule(16 * Settings.WorldHeight * 16, 8192);
        //    jobHandle2.Complete();
        //    Debug.Log(((Time.realtimeSinceStartup - start) * 1000f) + "ms");
        //}
        #endregion
    }
}
