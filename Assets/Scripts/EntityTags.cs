using Unity.Entities;
using Unity.Mathematics;

public struct Player : IComponentData
{
    public int chunkX;
    public int chunkY;
    public int chunkZ;

    public float3 position;

    public int renderDistance;
}
