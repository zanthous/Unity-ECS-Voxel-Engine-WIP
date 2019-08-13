using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTesitng : MonoBehaviour
{
    private int seed;
    public float maxHeight = 64;
    private int xRes;
    private int yRes;

    private float[,] terrainValues;

    [SerializeField] private Terrain terrain;
    void Start()
    {
        //xRes = terrain.terrainData.heightmapWidth;
        //yRes = terrain.terrainData.heightmapHeight;

        xRes = 256;
        yRes = 256;

        seed = Random.Range(int.MinValue/100, int.MaxValue/100);
        for(int x = 0; x < 100; x++)
        {
            for(int y = 0; y < 100; y++)
            {
                Debug.Log(64 + (int)(Mathf.PerlinNoise(x/50.0f, y/50.0f) * 30) );
            }
        }
    }
    
}
