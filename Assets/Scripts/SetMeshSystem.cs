using System.Collections.Generic;
using BovineLabs.Common.Native;
using BovineLabs.Common.Utility;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/// <summary>
///     Updates a mesh.
/// </summary>
/// 
public class MeshSystem : ComponentSystem
{
    //rendermesh
    private readonly List<Vector3> verticesList = new List<Vector3>();
    private readonly List<Vector3> normalsList = new List<Vector3>();
    private readonly List<Vector3> uvsList = new List<Vector3>();
    private readonly List<int> trianglesList = new List<int>();

    //pure data
    //private Dictionary<Entity, List<Vector3>> verticesList = new Dictionary<Entity, List<Vector3>>();
    //private Dictionary<Entity, List<Vector3>> normalsList = new Dictionary<Entity, List<Vector3>>();
    //private Dictionary<Entity, List<Vector3>> uvsList = new Dictionary<Entity, List<Vector3>>();
    //private Dictionary<Entity, List<int>> trianglesList = new Dictionary<Entity, List<int>>();

    //mesh
    public static Dictionary<Entity, Mesh> meshes = new Dictionary<Entity, Mesh>();

    private EntityQuery meshDirtyQuery;

    //private NativeArray<ArchetypeChunk> chunks;
    //private ArchetypeChunkComponentType<MeshDirty> meshDirtyType;

    //private ArchetypeChunkComponentType<MeshDirty> meshDirtyType;
    public class RemoveMeshDirtyECB : EntityCommandBufferSystem { }
    RemoveMeshDirtyECB system; 
    /// <inheritdoc/>
    protected override void OnCreateManager()
    {
        meshDirtyQuery = GetEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<MeshDirty>()
            },
            Any = System.Array.Empty<ComponentType>(),
            None = System.Array.Empty<ComponentType>()
        });
    }

    /// <inheritdoc/>
    unsafe protected override void OnUpdate()
    {
        if(!Main.InitDone)
            return;

        //var mat = Resources.Load<Material>("Materials/New Material");
        var chunks = this.meshDirtyQuery.CreateArchetypeChunkArray(Allocator.TempJob);

        var meshDirtyType = this.GetArchetypeChunkComponentType<MeshDirty>(true);
        
        for(var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            var chunk = chunks[chunkIndex];
            var meshDirty = chunk.GetNativeArray(meshDirtyType);

            #region rendermesh
            //for(var index = 0; index < chunk.Count; index++)
            //{
            //    var entity = meshDirty[index].Entity;
            //    Mesh mesh = this.EntityManager.GetSharedComponentData<RenderMesh>(entity).mesh;
            //    if(this.EntityManager.HasComponent<RenderMesh>(entity))
            //    {
            //        var meshInstanceRenderer =
            //            this.EntityManager.GetSharedComponentData<RenderMesh>(entity);

            //        this.SetMesh(
            //            entity,
            //            this.EntityManager.GetBuffer<Vertex>(entity).Reinterpret<Vector3>(),
            //            this.EntityManager.GetBuffer<Uv>(entity).Reinterpret<Vector3>(),
            //            this.EntityManager.GetBuffer<Normal>(entity).Reinterpret<Vector3>(),
            //            this.EntityManager.GetBuffer<Triangle>(entity).Reinterpret<int>());
            //    }
            //    else
            //    {
            //        chunks.Dispose();
            //        return;
            //    }
            //    PostUpdateCommands.RemoveComponent<MeshDirty>(meshDirty[index].Entity);
            //    PostUpdateCommands.AddComponent<ChunkUpToDate>(meshDirty[index].Entity);
            //    //PostUpdateCommands.SetSharedComponent<RenderMesh>(entity, new RenderMesh { mesh = mesh, material = mat });
            //}
            #endregion
            #region mesh

            for(int i = 0; i < chunk.Count; i++)
            {
                meshes[meshDirty[i].Entity].Clear();
            }

            int counter = 0;
            for(var index = 0; index < chunk.Count; index++)
            {
                counter++;
                var entity = meshDirty[index].Entity;
                
                this.SetMesh(
                    entity,
                    this.EntityManager.GetBuffer<Vertex>(entity).Reinterpret<Vector3>(),
                    this.EntityManager.GetBuffer<Uv>(entity).Reinterpret<Vector3>(),
                    this.EntityManager.GetBuffer<Normal>(entity).Reinterpret<Vector3>(),
                    this.EntityManager.GetBuffer<Triangle>(entity).Reinterpret<int>());

                PostUpdateCommands.RemoveComponent<MeshDirty>(meshDirty[index].Entity);
                PostUpdateCommands.AddComponent<ChunkUpToDate>(meshDirty[index].Entity);
            }
            #endregion

        }
        chunks.Dispose();
    }

    private void SetMesh(
        Entity e,
        DynamicBuffer<Vector3> vertices,
        DynamicBuffer<Vector3> uvs,
        DynamicBuffer<Vector3> normals,
        DynamicBuffer<int> triangles)
    {
        if(vertices.Length == 0)
        {
            //Debug.Log("Vertices length was 0?");
            return;
        }

        //rendermesh

        //mesh.SetVertices(this.verticesList);
        //mesh.SetNormals(this.normalsList);
        //mesh.SetUVs(0, this.uvsList);
        //mesh.SetTriangles(this.trianglesList, 0);

        //this.verticesList.Clear();
        //this.normalsList.Clear();
        //this.uvsList.Clear();
        //this.trianglesList.Clear();

        //pure data
        //this.verticesList[e].Clear();
        //this.uvsList[e].Clear();
        //this.normalsList[e].Clear();
        //this.trianglesList[e].Clear();

        //this.verticesList[e].Resize<Vector3>(vertices.Length);
        //this.uvsList[e].Resize<Vector3>(uvs.Length);
        //this.normalsList[e].Resize<Vector3>(normals.Length);
        //this.trianglesList[e].Resize<int>(triangles.Length);

        //this.verticesList[e].AddRange<Vector3>(vertices);
        //this.uvsList[e].AddRange<Vector3>(uvs);
        //this.normalsList[e].AddRange<Vector3>(normals);
        //this.trianglesList[e].AddRange<int>(triangles);

        //mesh
        verticesList.AddRange<Vector3>(vertices);
        uvsList.AddRange<Vector3>(uvs);
        normalsList.AddRange<Vector3>(normals);
        trianglesList.AddRange<int>(triangles);

        meshes[e].SetVertices(verticesList);
        meshes[e].SetUVs(0,uvsList);
        meshes[e].SetNormals(normalsList);
        meshes[e].SetTriangles(trianglesList,0);

        verticesList.Clear();
        normalsList.Clear();
        uvsList.Clear();
        trianglesList.Clear();
    }
}