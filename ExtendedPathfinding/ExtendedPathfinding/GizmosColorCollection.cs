using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    [CreateAssetMenu(fileName = "GizmosColorCollection", menuName = "ScriptableObjects/GizmosColorCollection", order = 1)]
    public class GizmosColorCollection : ScriptableObject
    {
        public List<Color> gizmosColors = new List<Color>();

        public Color GetColor(int value)
        {
            if (value < 0)
                return (Color.black);

            int lerpIndex = Mathf.RoundToInt(Mathf.Lerp(0f, (float)(gizmosColors.Count - 1), (float)(value) / gizmosColors.Count));
            if (lerpIndex < gizmosColors.Count)
                return gizmosColors[lerpIndex];
            else
                return (gizmosColors.Last());
        }
    }
}
