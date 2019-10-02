using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings 
{
    //do not change, code is not able to 
    public static int ChunkSize = 16;

    public static int WorldHeight = 128;

#if UNITY_EDITOR
    public static int RenderDistance = 4;
#else
    public static int RenderDistance = 16;
#endif
    public static bool chunksDirtyDebug = false;
}
