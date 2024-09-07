using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    [System.Serializable]
    public class PathInfo : MonoBehaviour
    {
        public GameObject pathSource;
        public GameObject pathTarget;

        public List<NodeInfo> nodes;

        public PathInfo(List<NodeInfo> newNodes, GameObject newSource, GameObject newTarget)
        {
            nodes = newNodes;
            pathSource = newSource;
            pathTarget = newTarget;
        }
    }

    public struct Evaluation
    {
        public int pathScore;
        public Dictionary<NodeInfo, int> nodeScores;
        public Dictionary<NodeInfo, int> nodeContextualScores;
        public Vector2 contextMinMax;
        private PathInfo pathInfo;

        public Evaluation(PathInfo newPathInfo, List<PathInfo> primaryPaths, List<PathInfo> secondaryPaths, NodeEvaluationType evaluationType)
        {
            pathInfo = newPathInfo;
            pathScore = 0;
            nodeScores = new Dictionary<NodeInfo, int>();
            nodeContextualScores = new Dictionary<NodeInfo, int>();
            contextMinMax = new Vector2(-1, 0);

            foreach (NodeInfo node in pathInfo.nodes)
            {
                nodeScores.Add(node, -1);
                nodeContextualScores.Add(node, -1);
            }

            if (evaluationType.HasFlag(NodeEvaluationType.Depth))
            {
                contextMinMax = new Vector2(0, pathInfo.nodes.Count);
                foreach (NodeInfo nodeInfo in pathInfo.nodes)
                {
                    nodeScores[nodeInfo] = pathInfo.nodes.IndexOf(nodeInfo);
                    pathScore += pathInfo.nodes.IndexOf(nodeInfo);
                }
                nodeContextualScores = new Dictionary<NodeInfo, int>(nodeScores);
            }
            if (evaluationType.HasFlag(NodeEvaluationType.Usage))
            {
                Dictionary<NodeInfo, int> overlapDict = NewNodeManager.GetOverlapCount(primaryPaths, primaryPaths);
                foreach (NodeInfo nodeInfo in pathInfo.nodes)
                {
                    if (overlapDict.ContainsKey(nodeInfo))
                    {
                        nodeScores[nodeInfo] = overlapDict[nodeInfo];
                        pathScore += overlapDict[nodeInfo];
                    }
                    else
                        nodeScores[nodeInfo] = 0;
                }
                nodeContextualScores.Clear();
                foreach (KeyValuePair<NodeInfo, int> kvp in overlapDict.OrderByDescending(n => n.Value))
                    nodeContextualScores.Add(kvp.Key, kvp.Value);
            }
            if (evaluationType.HasFlag(NodeEvaluationType.Heat))
            {
                Dictionary<NodeInfo, int> overlapDict = NewNodeManager.GetOverlapCount(primaryPaths, secondaryPaths);
                foreach (NodeInfo nodeInfo in pathInfo.nodes)
                {
                    if (overlapDict.ContainsKey(nodeInfo))
                    {
                        nodeScores[nodeInfo] = overlapDict[nodeInfo];
                        pathScore += overlapDict[nodeInfo];
                    }
                    else
                        nodeScores[nodeInfo] = 0;
                }
                nodeContextualScores.Clear();
                foreach (KeyValuePair<NodeInfo, int> kvp in overlapDict.OrderByDescending(n => n.Value))
                    nodeContextualScores.Add(kvp.Key, kvp.Value);
            }
            if (evaluationType.HasFlag(NodeEvaluationType.Distance))
            {
                foreach (NodeInfo nodeInfo in pathInfo.nodes)
                    nodeScores[nodeInfo] = Mathf.RoundToInt((Vector3.Distance(nodeInfo.nodeObject.transform.position, pathInfo.pathTarget.transform.position)));
                pathScore = (int)nodeScores.Values.Average();
                contextMinMax = new Vector2(nodeScores.Values.Min(), nodeScores.Values.Max());

                nodeContextualScores.Clear();
                foreach (KeyValuePair<NodeInfo, int> kvp in nodeScores.OrderBy(n => n.Value))
                    nodeContextualScores.Add(kvp.Key, kvp.Value);

            }
        }

        public void GetContextualScore(NodeInfo node)
        {
            nodeContextualScores = new Dictionary<NodeInfo, int>();

        }
    }
}
