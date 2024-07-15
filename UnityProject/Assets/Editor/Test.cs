using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Test
{
    [MenuItem("MyMenu/LayerTest")]
    static void DebugTest()
    {

        List<int> layers = new List<int>();
        var bitmask = 1107298561;
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & bitmask) != 0)
            {
                layers.Add(i);
            }
        }

        foreach (int layerMask in layers)
            Debug.Log("Layer: " + LayerMask.LayerToName(layerMask));
    }
}
