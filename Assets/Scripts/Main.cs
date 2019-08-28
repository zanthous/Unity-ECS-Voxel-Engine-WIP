using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
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
    private Plane[] frustumPlanes = new Plane[6];

    [SerializeField] private Texture2D[] textures;

    //[SerializeField] private Camera camera;

    bool doOnce = true;
    
    EntityManager em;

    public static NativeHashMap<int3, Entity> chunkEntities;

    static public bool InitDone { get => initDone; }

    //private InstancedRenderMeshBatchGroup instancedRenderMeshBatchGroup;

    private void OnDestroy()
    {
        chunkEntities.Dispose();
    }

    void Awake()
    {
        //TODO URGENT find a better way to deal with this
        chunkEntities =
        new NativeHashMap<int3, Entity>(Settings.RenderDistance * Settings.RenderDistance * (Settings.WorldHeight / Settings.ChunkSize) * 4 * 100,
            Allocator.Persistent);

        em = World.Active.EntityManager;

        var blockChunk = em.CreateArchetype(
            ComponentType.ReadWrite<Translation>(),
            //ComponentType.ReadWrite<LocalToWorld>(),
            //ComponentType.ReadWrite<RenderMesh>(),
            //ComponentType.ReadWrite<Scale>(),
            ComponentType.ReadWrite<Chunk>(),
            ComponentType.ReadOnly<Ungenerated>(),
            ComponentType.ReadOnly<ShouldDraw>());

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
        //World.Active.GetOrCreateSystem<DrawMeshSystem>();
        World.Active.CreateSystem<DrawMeshSystem>();
        World.Active.GetExistingSystem<DrawMeshSystem>().Enabled = true;
        //Debug.Log(World.Active.GetExistingSystem<DrawMeshSystem>());
        //chunkDataGenerationSystem = World.Active.GetOrCreateSystem<ChunkBlockIDFill>();
        //chunkMeshBufferFillSystem = World.Active.GetOrCreateSystem<ChunkMeshGenerationSystem>();
        //meshSetSystem = World.Active.GetOrCreateSystem<MeshSystem>();
        initDone = true;
        //World.Active.DestroySystem();
        World.Active.GetExistingSystem<PresentationSystemGroup>().Enabled = false;

        //Morton curve code port testing
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


        //Create texture2darray
        //Texture2DArray t = new Texture2DArray(32, 32, textures.Length, 
        //    UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
        //for(int i = 0; i < textures.Length; i++)
        //{
        //    t.SetPixels(textures[i].GetPixels(),i);
        //}

        //t.Apply();
        
        //AssetDatabase.CreateAsset(t, "Assets/Sprites/TerrainAtlas.asset");

        material = Resources.Load<Material>("Materials/ArrayTexture");
        //material.SetTexture("_Textures", t);
    }

    private void FixedUpdate()
    {
        //Debug.Log(chunkEntities.Length);
    }

    private void Update()
    {
        World.Active.GetExistingSystem<DrawMeshSystem>().Update();
        

        //frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        //int3 chunkPos;
        ////Just getting the components takes 2.5 ms
        //foreach(Entity e in entitySet)
        //{
        //    if(!em.HasComponent<ChunkUpToDate>(e))
        //        continue;
        //    chunkPos = em.GetComponentData<Chunk>(e).pos;
        //    if(GeometryUtility.TestPlanesAABB(frustumPlanes, new Bounds())
        //    Graphics.DrawMesh(MeshSystem.meshes[e], em.GetComponentData<Translation>(e).Value, Quaternion.identity, material, 0);
        //}
    }

    private void LateUpdate()
    {
        World.Active.GetExistingSystem<DrawMeshSystem>().CompleteJob();
        int3 chunkPos;
        foreach(Entity e in entitySet)
        {
            if(em.HasComponent<ShouldDraw>(e) && em.GetComponentData<ShouldDraw>(e).Value == true && em.HasComponent<ChunkUpToDate>(e))
            { 
                chunkPos = em.GetComponentData<Chunk>(e).pos;
                Graphics.DrawMesh(MeshSystem.meshes[e], new Vector3(chunkPos.x*16, chunkPos.y * 16, chunkPos.z * 16), Quaternion.identity, material, 0);
            }
        }
    }
}
