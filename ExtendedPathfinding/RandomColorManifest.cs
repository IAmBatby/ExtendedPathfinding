using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding
{
    [CreateAssetMenu(fileName = "RandomColorManifest", menuName = "ScriptableObjects/ExtendedPathfinding/RandomColorManifest", order = 1)]
    public class RandomColorManifest : ScriptableObject
    {
        public List<Color> allColors;

        public Color GetColor(int index)
        {
            if (allColors.Count > index)
                return (allColors[index]);
            else
                return (Color.black);
        }
    }
}
