using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings 
{
    //do not change, code is not able to 
    public static int ChunkSize = 16;

    public static int WorldHeight = 128;
    //TODO if all math is wrong it's because I set renderdistance to twice of what it was before
    public static int RenderDistance = 16;
    
    public static bool chunksDirtyDebug = false;
}
