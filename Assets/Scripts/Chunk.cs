using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
public struct Chunk : IComponentData
{
    public int3 pos;
}

public struct ShouldDraw : IComponentData
{
    public bool Value;
}

[InternalBufferCapacity(16 * 16 * 16)]
public struct BlockIDBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator ushort(BlockIDBuffer e) { return e.Value; }
    public static implicit operator BlockIDBuffer(ushort e) { return new BlockIDBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public ushort Value;
}

[InternalBufferCapacity(0)]
public struct Vertex : IBufferElementData
{
    public static implicit operator float3(Vertex e) { return e.Value; }
    public static implicit operator Vertex(float3 e) { return new Vertex { Value = e }; }
    public float3 Value;
}

//This will be revisited whenever unity update their mesh api
//and I can use 10/10/10/2 formats
[InternalBufferCapacity(0)]
public unsafe struct VertexAttribute : IBufferElementData
{
    //public static implicit operator float3(VertexAttribute e) { return e.Value; }
    //public static implicit operator VertexAttribute(float3 e) { return new VertexAttribute { Value = e }; }
    public ushort Uv;   
    public int Normal;
    public half4 Vertex;
}

[InternalBufferCapacity(0)]
public struct Uv : IBufferElementData
{
    public static implicit operator float3(Uv e) { return e.Value; }
    public static implicit operator Uv(float3 e) { return new Uv { Value = e }; }
    public float3 Value;
}

[InternalBufferCapacity(0)]
public struct Normal : IBufferElementData
{
    public static implicit operator float3(Normal e) { return e.Value; }
    public static implicit operator Normal(float3 e) { return new Normal { Value = e }; }
    public float3 Value;
}


[InternalBufferCapacity(0)]
public struct Triangle : IBufferElementData
{
    public static implicit operator int(Triangle e) { return e.Value; }
    public static implicit operator Triangle(int e) { return new Triangle { Value = e }; }
    public int Value;
}
[Serializable]
public struct MeshDirty : IComponentData
{
    public Entity Entity;
}

public struct ChunkUpToDate : IComponentData { }

public struct SharedMaterial : ISharedComponentData
{

}