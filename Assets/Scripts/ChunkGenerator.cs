using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;

//Everything here was replaced or didn't work properly


//EntityArchetype chunkArchetype;
//EntityQuery group;
//BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;



//[BurstCompile]
//struct PopulateChunkJob : IJob
//{
//    public NativeArray<ushort> blocks;
//    public void Execute()
//    {
//        //blocks = new NativeArray<ushort>(16*Settings.WorldHeight*16, Allocator.Persistent);
//        for(int x = 0; x < 16; ++x)
//        {
//            for(int y = 0; y < 256; ++y)
//            {
//                for(int z = 0; z < 16; ++z)
//                {
//                    blocks[x + 16 * z + 16 * y * 16] = y > 60 ? (ushort) 1 : (ushort) 0;
//                }
//            }
//        }
//        //commandBuffer.RemoveComponent<Chunk>(chunks[chunkIndex])
//    }
//}

//[BurstCompile]
//struct PopulateChunkJobParallel : IJobParallelFor
//{
//    public DynamicBuffer<BlockIDBuffer> blocks;
//    [ReadOnly] public Translation chunkPos;
//    public void Execute(int i)
//    {
//        int x, y, z;
//        //for(int i = 0; i < 16*Settings.WorldHeight*16; ++i)
//        //{
//        //256*16 = 4096 >> 12 = 1
//        x = i >> 4;
//        //256 >> 
//        y = i >> 12;
//        z = i >> 8;
//        blocks[i] = chunkPos.Value.y * 16 + y > 60 ? (ushort) 1 : (ushort) 0;
//        //}
//    }
//}