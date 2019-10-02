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
    //https://forum.unity.com/threads/lists-of-things-collections-arrays-groups.524532/

    static private bool initDone = false;

    private readonly int ChunkCount = (Settings.RenderDistance * 2) * (Settings.RenderDistance * 2) * (Settings.WorldHeight / Settings.ChunkSize);

    private ChunkBlockIDFill chunkDataGenerationSystem;
    private ChunkMeshGenerationSystem chunkMeshBufferFillSystem;
    private MeshSystem meshSetSystem;
    private NativeArray<Entity> entitySet;
    private Material material;
    private Plane[] frustumPlanes = new Plane[6];

    private DrawMeshSystem drawMeshSystemRef;

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

        entitySet =
        new NativeArray<Entity>(
            (Settings.RenderDistance * 2) * (Settings.RenderDistance * 2) * (Settings.WorldHeight / Settings.ChunkSize),
            Allocator.Persistent, NativeArrayOptions.UninitializedMemory);


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
            entitySet[i] = temp;

        }
        //World.Active.GetOrCreateSystem<DrawMeshSystem>();
        World.Active.CreateSystem<DrawMeshSystem>();
        World.Active.GetExistingSystem<DrawMeshSystem>().Enabled = true;

        World.Active.GetExistingSystem<SimulationSystemGroup>().AddSystemToUpdateList(World.Active.GetExistingSystem<DrawMeshSystem>());
        World.Active.GetExistingSystem<SimulationSystemGroup>().SortSystemUpdateList();

        initDone = true;
        //World.Active.DestroySystem();
        World.Active.GetExistingSystem<PresentationSystemGroup>().Enabled = false;

        //Morton curve code testing
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
        Texture2DArray t = new Texture2DArray(32, 32, textures.Length,TextureFormat.RGBA32,false);
        t.filterMode = FilterMode.Point;
        Debug.Log(t.graphicsFormat);
        for(int i = 0; i < textures.Length; i++)
        {
            t.SetPixels(textures[i].GetPixels(), i,0);
        }
        //var a = textures[3].GetPixels();
        //Debug.Log(t.GetPixels(3)[0]);

        t.Apply();

        Debug.Log("Created texture2darray at Assets/Sprites/TerrainAtlas.asset");
        AssetDatabase.CreateAsset(t, "Assets/Sprites/TerrainAtlas.asset");

        material = Resources.Load<Material>("Materials/ArrayTexture");
        material.SetTexture(0, t);
        //material.SetTexture("_Textures", t);
    }

    private void FixedUpdate()
    {
        //Debug.Log(chunkEntities.Length);
    }

    private void Update()
    {
        //drawMeshSystemRef.


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

        Entity e;
        //todo optimize
        //the non-graphics drawmesh parts are slow but not sure where exactly yet
        for(int i = 0; i < entitySet.Length; i++)
        {
            e = entitySet[i];
            if(em.HasComponent<ShouldDraw>(e) && em.GetComponentData<ShouldDraw>(e).Value == true && em.HasComponent<ChunkUpToDate>(e))
            {
                chunkPos = em.GetComponentData<Chunk>(e).pos;
                if(MeshSystem.meshes[e].vertexCount > 0)
                {
                    Graphics.DrawMesh(MeshSystem.meshes[e], new Vector3(chunkPos.x * 16, chunkPos.y * 16, chunkPos.z * 16), Quaternion.identity, material, 0);
                }
            }
        }
        //measure performance
        //for(int i = 0; i < entitySet.Length; i++)
        //{
        //    e = entitySet[i];
        //    if(em.HasComponent<ShouldDraw>(e) && em.GetComponentData<ShouldDraw>(e).Value == true && em.HasComponent<ChunkUpToDate>(e))
        //    {
        //    }
        //}
        //for(int i = 0; i < entitySet.Length; i++)
        //{
        //    e = entitySet[i];
        //    chunkPos = em.GetComponentData<Chunk>(e).pos;
        //    if(MeshSystem.meshes[e].vertexCount > 0)
        //    {

        //    }
        //}
        //todo clean chunkentities
        //if(chunkEntities.Length > ChunkCount*10)
        //{
        //    
        //}
    }
}
