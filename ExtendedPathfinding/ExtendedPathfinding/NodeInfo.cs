using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    public enum NodeValue { Unused, Active }
    [System.Serializable]
    public class NodeInfo
    {
        public GameObject nodeObject;
        public NodeValue nodeValue = NodeValue.Unused;

        public NodeInfo(GameObject newNodeObject)
        {
            nodeObject = newNodeObject;
        }
    }

}
