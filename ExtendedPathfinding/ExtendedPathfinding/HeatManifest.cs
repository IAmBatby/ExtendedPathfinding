using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    [CreateAssetMenu(fileName = "HeatManifest", menuName = "ScriptableObjects/ExtendedPathfinding/HeatManifest", order = 1)]
    public class HeatManifest : ScriptableObject
    {
        [Range(0, 255)]
        public int opacity;
        public List<ColorWithDistance> colorWithDistances;

        public Color GetThresholdColor(float distance)
        {
            ColorWithDistance previousColorWithDistance = null;
            foreach (ColorWithDistance colorWithDistance in colorWithDistances)
            {
                if (distance < colorWithDistance.distance)
                {
                    Color color;
                    if (previousColorWithDistance != null)
                    {
                        float t = Mathf.InverseLerp(previousColorWithDistance.distance, colorWithDistance.distance, distance);
                        color = Color.Lerp(colorWithDistance.color, previousColorWithDistance.color, t);
                    }
                    else
                        color = colorWithDistance.color;

                    return (new Color(color.r, color.g, color.b, opacity));
                }
                previousColorWithDistance = colorWithDistance;
            }

            return (Color.black);
        }
    }

    [System.Serializable]
    public class ColorWithDistance
    {
        public Color color;
        public float distance;
    }
}
