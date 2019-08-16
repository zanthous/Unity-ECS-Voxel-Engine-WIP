using System.Collections; 
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs; 
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;

[DisableAutoCreation]
public class DrawMeshSystem : JobComponentSystem
{
    private static Material material;
    private Camera camera;
    private NativeArray<float4> planes;
    private JobHandle jobHandle;

    protected override void OnCreate()
    {
        material = Resources.Load<Material>("Materials/New Material");
        camera = GameObject.FindObjectOfType<Camera>();
        planes = new NativeArray<float4>(6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    protected override void OnDestroy()
    {
        planes.Dispose();
    }
    

    unsafe protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        //https://gamedev.stackexchange.com/questions/156743/finding-the-normals-of-the-planes-of-a-view-frustum

        var proj = camera.projectionMatrix;
        var view = camera.worldToCameraMatrix;
        var mat = proj * view;

        //L,R,D,U,N,F
        //planes[0] = new float4(
        //mat[0, 0] + mat[3, 0],
        //mat[0, 1] + mat[3, 1],
        //mat[0, 2] + mat[3, 2],
        //mat[0, 3] + mat[3, 3]);

        //planes[1] = new float4(
        //-mat[0, 0] + mat[3, 0],
        //-mat[0, 1] + mat[3, 1],
        //-mat[0, 2] + mat[3, 2],
        //-mat[0, 3] + mat[3, 3]);

        //planes[2] = new float4(
        //mat[1, 0] + mat[3, 0],
        //mat[1, 1] + mat[3, 1],
        //mat[1, 2] + mat[3, 2],
        //mat[1, 3] + mat[3, 3]);

        //planes[3] = new float4(
        //-mat[1, 0] + mat[3, 0],
        //-mat[1, 1] + mat[3, 1],
        //-mat[1, 2] + mat[3, 2],
        //-mat[1, 3] + mat[3, 3]);

        //planes[4] = new float4(
        //mat[2, 0] + mat[3, 0],
        //mat[2, 1] + mat[3, 1],
        //mat[2, 2] + mat[3, 2],
        //mat[2, 3] + mat[3, 3]);

        //planes[5] = new float4(
        //-mat[2, 0] + mat[3, 0],
        //-mat[2, 1] + mat[3, 1],
        //-mat[2, 2] + mat[3, 2],
        //-mat[2, 3] + mat[3, 3]);

        planes[0] = new float4(
        mat[0, 0] + mat[3, 0],
        mat[0, 1] + mat[3, 1],
        mat[0, 2] + mat[3, 2],
        mat[0, 3] + mat[3, 3]);


        planes[1] = new float4(
        -mat[0, 0] + mat[3, 0],
        -mat[0, 1] + mat[3, 1],
        -mat[0, 2] + mat[3, 2],
        -mat[0, 3] + mat[3, 3]);

        planes[2] = new float4(
        mat[1, 0] + mat[3, 0],
        mat[1, 1] + mat[3, 1],
        mat[1, 2] + mat[3, 2],
        mat[1, 3] + mat[3, 3]);

        planes[3] = new float4(
        -mat[1, 0] + mat[3, 0],
        -mat[1, 1] + mat[3, 1],
        -mat[1, 2] + mat[3, 2],
        -mat[1, 3] + mat[3, 3]);

        planes[4] = new float4(
        mat[2, 0] + mat[3, 0],
        mat[2, 1] + mat[3, 1],
        mat[2, 2] + mat[3, 2],
        mat[2, 3] + mat[3, 3]);

        //planes[0] /= Mathf.Sqrt(planes[0].x* planes[0].x + planes[0].y * planes[0].y + planes[0].z* planes[0].z + planes[0].w* planes[0].w);

        //Debug.Log(planes[0]);
        //Debug.Log(planes[0].x + " " + planes[0].y + " " + planes[0].z + " " + planes[0].w + " ");
        //Vector3 pos = new Vector3(-16, 0, -16);
        //Debug.Log(planes[0].x * pos.x + planes[0].y * pos.y + planes[0].z * pos.z + planes[0].w);
        //planes = GeometryUtility.CalculateFrustumPlanes(camera);
        var job = new DrawMeshJob
        {
            p = planes
        };
        jobHandle = job.Schedule<DrawMeshJob>(this, inputDeps);
        return jobHandle;
        //Donotautocreate
        //start in update complete in lateupdate
        //call draw

    }

    public void CompleteJob()
    {
        jobHandle.Complete();
    }
    

    [RequireComponentTag(typeof(ChunkUpToDate))]
    [BurstCompile]
    unsafe struct DrawMeshJob : IJobForEachWithEntity<Chunk, ShouldDraw>
    {
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<float4> p;

        public void Execute(Entity entity, int index, [ReadOnly] ref Chunk c2, ref ShouldDraw draw)
        {
            int insideCount = 0;
            draw.Value = false;
            int3 pos = new int3((c2.pos.x << 4), (c2.pos.y << 4), (c2.pos.z << 4));

            //NativeArray<float3> positions = new NativeArray<float3>(8,Allocator.Temp,NativeArrayOptions.UninitializedMemory);
            //for(int i = 0; i < 8; i++)
            //{
            //    positions[i] = new float3(pos.x + ((i & 1) << 4), pos.y + (((i >> 1) & 1) << 4), pos.z + (((i >> 2) & 1) << 4));
            //}

            int3 pos0 = new int3(pos.x + ((0 & 1) << 4), pos.y + (((0 >> 1) & 1) << 4), pos.z + (((0 >> 2) & 1) << 4));
            int3 pos1 = new int3(pos.x + ((1 & 1) << 4), pos.y + (((1 >> 1) & 1) << 4), pos.z + (((1 >> 2) & 1) << 4));
            int3 pos2 = new int3(pos.x + ((2 & 1) << 4), pos.y + (((2 >> 1) & 1) << 4), pos.z + (((2 >> 2) & 1) << 4));
            int3 pos3 = new int3(pos.x + ((3 & 1) << 4), pos.y + (((3 >> 1) & 1) << 4), pos.z + (((3 >> 2) & 1) << 4));
            int3 pos4 = new int3(pos.x + ((4 & 1) << 4), pos.y + (((4 >> 1) & 1) << 4), pos.z + (((4 >> 2) & 1) << 4));
            int3 pos5 = new int3(pos.x + ((5 & 1) << 4), pos.y + (((5 >> 1) & 1) << 4), pos.z + (((5 >> 2) & 1) << 4));
            int3 pos6 = new int3(pos.x + ((6 & 1) << 4), pos.y + (((6 >> 1) & 1) << 4), pos.z + (((6 >> 2) & 1) << 4));
            int3 pos7 = new int3(pos.x + ((7 & 1) << 4), pos.y + (((7 >> 1) & 1) << 4), pos.z + (((7 >> 2) & 1) << 4));

            //Vector3 max = new Vector3((c2.pos.x << 4) + 8, (c2.pos.y << 4) + 8, (c2.pos.z << 4) + 8);
            //if I single vertex is inside the frustum it should be drawn
            //skipping far plane for now
            for(int j = 0; j < 5; j++)
            {
                if(p[j].x * pos0.x + p[j].y * pos0.y + p[j].z * pos0.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos1.x + p[j].y * pos1.y + p[j].z * pos1.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos2.x + p[j].y * pos2.y + p[j].z * pos2.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos3.x + p[j].y * pos3.y + p[j].z * pos3.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos4.x + p[j].y * pos4.y + p[j].z * pos4.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos5.x + p[j].y * pos5.y + p[j].z * pos5.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos6.x + p[j].y * pos6.y + p[j].z * pos6.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }
                if(p[j].x * pos7.x + p[j].y * pos7.y + p[j].z * pos7.z + p[j].w > 0)
                {
                    insideCount++;
                    continue;
                }

            }
            if(insideCount==5)
            {
                draw.Value = true;
            }
        }
    }
}
