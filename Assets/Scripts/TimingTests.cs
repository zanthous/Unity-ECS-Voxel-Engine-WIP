using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

public class TimingTests : MonoBehaviour
{  
    private string report;

    void Start()
    {
        //Uncomment to test
        //StartCoroutine(Wait5Seconds());
    }

    private IEnumerator Wait5Seconds()
    {
        Debug.Log("Waiting");
        yield return new WaitForSeconds(5);
        Debug.Log("Doing test");
        DoTest();
    }

    private void DoTest()
    {

        var stopwatch = new System.Diagnostics.Stopwatch();
        int sum;
        //int size = 50;
        int repetitions = 200;
        var array = new int[16][][];
        NativeArray<int> nativeArray = new NativeArray<int>(16 * 256 * 16,Allocator.Persistent);
        var flatArray = new int[16 * 256 * 16];
        

        for(var i = 0; i < 16; ++i)
        {
            array[i] = new int[256][];
            for(int j = 0; j < 256; ++j)
            {
                array[i][j] = new int[16];
                for(int k = 0; k < 16; ++k)
                {
                    array[i][j][k] = i + 16 * k + 16 * j * 16;
                    flatArray[i + 16 * k + 16 * j * 16] = i + 16 * k + 16 * j * 16;
                    flatArray[i + 16 * k + 16 * j * 16] = i + 16 * k + 16 * j * 16;
                }
            }
        }

        //native

        stopwatch.Reset();
        stopwatch.Start();
        sum = 0;
        for(int r = 0; r < repetitions; r++)
        {
            for(var i = 0; i < 16; ++i)
            {
                for(int j = 0; j < 256; ++j)
                {
                    for(int k = 0; k < 16; ++k)
                    {
                        sum += nativeArray[i + 16 * k + 16 * j * 16];
                    }
                }
            }
        }

        var nativeArrayRead = stopwatch.ElapsedMilliseconds;

        stopwatch.Reset();
        stopwatch.Start();
        for(int r = 0; r < repetitions; r++)
        {
            for(var i = 0; i < 16; ++i)
            {
                for(int j = 0; j < 256; ++j)
                {
                    for(int k = 0; k < 16; ++k)
                    {
                        nativeArray[i + 16 * k + 16 * j * 16] = i;
                    }
                }
            }
        }
        
        var nativeArrayWrite = stopwatch.ElapsedMilliseconds;

        //flat
        stopwatch.Reset();
        stopwatch.Start();
        sum = 0;
        for(int r = 0; r < repetitions; r++)
        {
            for(var i = 0; i < 16; ++i)
            {
                for(int j = 0; j < 256; ++j)
                {
                    for(int k = 0; k < 16; ++k)
                    {
                        sum += flatArray[i + 16 * k + 16 * j * 16];
                    }
                }
            }
        }

        var flatArrayRead = stopwatch.ElapsedMilliseconds;

        stopwatch.Reset();
        stopwatch.Start();
        for(int r = 0; r < repetitions; r++)
        {
            for(var i = 0; i < 16; ++i)
            {
                for(int j = 0; j < 256; ++j)
                {
                    for(int k = 0; k < 16; ++k)
                    {
                        flatArray[i + 16 * k + 16 * j * 16] = i;
                    }
                }
            }
        }


        var flatArrayWrite = stopwatch.ElapsedMilliseconds;

        stopwatch.Reset();
        stopwatch.Start();
        sum = 0;
        for(int r = 0; r < repetitions; r++)
        {
            for(var i = 0; i < 16; ++i)
            {
                for(int j = 0; j < 256; ++j)
                {
                    for(int k = 0; k < 16; ++k)
                    {
                        sum += array[i][j][k];
                    }
                }
            }
        }
        var otherArrayRead = stopwatch.ElapsedMilliseconds;

        stopwatch.Reset();
        stopwatch.Start();
        for(int r = 0; r < repetitions; r++)
        {
            for(var i = 0; i < 16; ++i)
            {
                for(int j = 0; j < 256; ++j)
                {
                    for(int k = 0; k < 16; ++k)
                    {
                        array[i][j][k] = i;
                    }
                }
            }
        }
        var otherArrayWrite = stopwatch.ElapsedMilliseconds;

        report =
            "Test,Array Time,List Time\n"
            + "Read," + flatArrayRead + "," + otherArrayRead + "\n"
            + "Write," + flatArrayWrite + "," + otherArrayWrite;
        //Debug.Log("Test,Array Time,List Time"
        //    + " Read," + flatArrayRead + "," + otherArrayRead + " "
        //    + " Write," + flatArrayWrite + "," + otherArrayWrite);
        Debug.Log("Read,Write");
        Debug.Log("Flat: " + flatArrayRead + "," + " " + flatArrayWrite);
        Debug.Log("Jagged: " + otherArrayRead + "," + " " + otherArrayWrite);
        Debug.Log("native: " + nativeArrayRead + "," + " " + nativeArrayWrite);

        

        nativeArray.Dispose();
    }

    void OnGUI()
    {
        GUI.TextArea(new Rect(0, 0, Screen.width, Screen.height), report);
    }
}