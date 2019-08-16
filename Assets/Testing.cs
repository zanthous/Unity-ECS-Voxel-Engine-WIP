using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Testing : MonoBehaviour
{
    private NativeHashMap<int3, int> test;


    private void OnDestroy()
    {
        test.Dispose();
    }


    // Start is called before the first frame update
    void Start()
    {
         test = new NativeHashMap<int3, int>(20, Allocator.Persistent);
        test.TryAdd(new int3(0, 0, 0), 0);
        test.TryAdd(new int3(1, 1, 1), 1);
        test.TryAdd(new int3(2, 2, 2), 2);
        test.TryAdd(new int3(3, 3, 3), 3);

        Debug.Log(test[new int3(0, 0, 0)]);
        Debug.Log(test[new int3(1, 1, 1)]);
        Debug.Log(test[new int3(2, 2, 2)]);
        Debug.Log(test[new int3(3, 3, 3)]);
    }
}
