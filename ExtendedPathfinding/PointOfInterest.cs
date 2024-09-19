using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding
{
    [System.Serializable]
    public class PointOfInterest
    {
        public GameObject InterestObject;
        public Color Color;

        public PointOfInterest(GameObject interestObject, Color color)
        {
            InterestObject = interestObject;
            Color = color;
        }
    }
}
