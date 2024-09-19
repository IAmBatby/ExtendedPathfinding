using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace ExtendedPathfinding
{
    public class ExtendedPathNode
    {
        public Vector3 Position { get; set; }
        public Color Color { get; set; }

        public ExtendedPathNode(Vector3 newPosition, Color newColor)
        {
            Position = newPosition;
            Color = newColor;
        }

        public ExtendedPathNode(Vector3 newPosition)
        {
            Position = newPosition;
            Color = Color.white;
        }
    }
}
