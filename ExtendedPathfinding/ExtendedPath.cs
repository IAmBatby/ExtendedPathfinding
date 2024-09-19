using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace ExtendedPathfinding
{
    public class ExtendedPath
    {
        public PointOfInterest SourcePoint;
        public PointOfInterest TargetPoint;
        public List<ExtendedPathNode> PathNodes;
        
        public ExtendedPath(PointOfInterest newSource, PointOfInterest newTarget, List<Vector3> newPositions, Color newColor)
        {
            SourcePoint = newSource;
            TargetPoint = newTarget;
            PathNodes = new List<ExtendedPathNode>();
            for (int i = 0; i < newPositions.Count; i++)
                PathNodes.Add(new ExtendedPathNode(newPositions[i], newColor));
        }

        public ExtendedPath(PointOfInterest newSource, PointOfInterest newTarget, NavMeshPath navMeshPath, Color newColor)
        {
            SourcePoint = newSource;
            TargetPoint = newTarget;
            PathNodes = new List<ExtendedPathNode>();
            for (int i = 0; i < navMeshPath.corners.Length; i++)
                PathNodes.Add(new ExtendedPathNode(navMeshPath.corners[i], newColor));
        }

        public List<Vector3> GetPositions()
        {
            List<Vector3> returnList = new List<Vector3>();
            foreach (ExtendedPathNode pathNode in PathNodes)
                returnList.Add(pathNode.Position);
            return (returnList);
        }

        public List<(Vector3, Color)> GetPositionsWithColor()
        {
            List<(Vector3, Color)> returnList = new List<(Vector3, Color)>();
            foreach (ExtendedPathNode pathNode in PathNodes)
                returnList.Add((pathNode.Position, pathNode.Color));
            return (returnList);
        }
    }
}
