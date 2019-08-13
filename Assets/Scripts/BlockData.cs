using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

//index everything by blockid
public static class BlockData
{
    public struct BlockDataS
    {
        public string _name;
        public bool _transparent;
        public bool _solid;

        public BlockDataS(string name_in, bool transparent_in, bool solid_in)
        {
            _transparent = transparent_in;
            _solid = solid_in;
            _name = name_in;
        }
    }

    public static BlockDataS[] blocks = new BlockDataS[]
    {
        //name                  transparency    solid
        new BlockDataS("air",   true,           false),
        new BlockDataS("grass", true,           false)
    };

    public static int transparent = 0x00000001;
    public static int solid = 0x00000003;
    //public static int transparent = 0x00000007;
    //public static int transparent = 0x0000000F;
    //public static int transparent = 0x0000001F;
    //public static int transparent = 0x0000003F;
    //public static int transparent = 0x0000007F;


    //I would like to try a system like this
    //I am assuming it will have good cache performance compared to alternatives
    //Although, I could also look into bitwise combination of multiple values potentially

    //Anyway it generates an array of ints based on the above struct of values by |'ing 
    //together the correct flags
    public static readonly NativeArray<int> flags = 
        new NativeArray<int>(
            blocks.Select(x => 
            (x._transparent ? transparent : 0) | 
            (x._solid ? solid : 0)
            ).ToArray(), Allocator.Persistent);
   // public static readonly NativeArray<bool> solid =
   //     new NativeArray<bool>(blocks.Select(x => x._solid).ToArray(), Allocator.Persistent);
    public static readonly string[] name = blocks.Select(x => x._name).ToArray();
}
