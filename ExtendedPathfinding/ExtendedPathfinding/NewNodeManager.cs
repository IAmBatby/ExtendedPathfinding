using System;
using System.Collections.Generic;
using System.Text;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    using HarmonyLib;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;


    public enum NodeEvaluationType
    {
        None, Depth, Usage, Heat, Distance
    }

    [System.Flags]
    public enum PathView
    {
        None = 0,
        Entrance = 1 << 0,
        Ship = 1 << 1,
        Connections = 1 << 2,
        Unused = 1 << 3
    }

    public class NewNodeManager : MonoBehaviour
    {
        [Header("Sources")]
        public SourceInfo shipInfo;
        public SourceInfo entranceInfo;

        [Space(10)]
        [Header("Viewing Settings")]

        public NodeEvaluationType nodeEvaluationTypeToggle;
        public PathView pathViewToggle;
        public float sphereRadius;

        [Space(10)]
        [Header("Specification Settings")]
        [Range(1, 15)]
        public int requiredBranchDepth;
        public int targetRouteCount;

        [Space(10)]
        [Header("References")]
        public GizmosColorCollection colorCollection;


        List<NodeInfo> allNodes = new List<NodeInfo>();
        public Dictionary<GameObject, NodeInfo> nodeInfoObjectDict = new Dictionary<GameObject, NodeInfo>();

        public void Start() { }

        public void OnDrawGizmos()
        {
            allNodes.Clear();
            nodeInfoObjectDict.Clear();
            foreach (GameObject aiNodeObject in GameObject.FindGameObjectsWithTag("OutsideAINode"))
            {
                NodeInfo newNodeInfo = new NodeInfo(aiNodeObject);
                allNodes.Add(newNodeInfo);
                nodeInfoObjectDict.Add(aiNodeObject, newNodeInfo);
            }

            List<PathInfo> shipPaths = CreatePathInfos(shipInfo, entranceInfo);
            List<PathInfo> entrancePaths = CreatePathInfos(entranceInfo, shipInfo);
            if (pathViewToggle.HasFlag(PathView.Ship))
                DrawPaths(shipPaths, entrancePaths);
            if (pathViewToggle.HasFlag(PathView.Entrance))
                DrawPaths(entrancePaths, shipPaths);
        }

        public List<PathInfo> CreatePathInfos(SourceInfo source, SourceInfo target)
        {
            Dictionary<int, List<NodeData>> nodeDict = new Dictionary<int, List<NodeData>>();

            List<GameObject> temporaryNodes = new List<GameObject>(allNodes.Select(n => n.nodeObject));

            nodeDict.Add(1, new List<NodeData>());
            foreach (NodeData priorityOneNode in GetNodesFromObject(source.sourceObject, new List<GameObject>(temporaryNodes), 1, source.distanceRange))
            {
                temporaryNodes.Remove(priorityOneNode.node);
                nodeDict[1].Add(priorityOneNode);
            }

            for (int priority = 2; priority < requiredBranchDepth + 2; priority++)
            {
                nodeDict.Add(priority, new List<NodeData>());
                foreach (NodeData previousNodeData in nodeDict[priority - 1])
                {
                    foreach (NodeData priorityNode in GetNodesFromObject(previousNodeData.node, new List<GameObject>(temporaryNodes), priority, source.distanceRange))
                    {
                        priorityNode.parentNode = previousNodeData;
                        temporaryNodes.Remove(priorityNode.node);
                        nodeDict[priority].Add(priorityNode);
                    }
                }
            }

            List<NodePath> nodePaths = new List<NodePath>();
            List<PathInfo> newPaths = new List<PathInfo>();

            int highestViablePriority = -1;
            for (int nodePriorityList = nodeDict.Count - 1; nodePriorityList > -1; nodePriorityList--)
            {
                if (nodeDict.ContainsKey(nodePriorityList) && nodeDict[nodePriorityList].Count >= targetRouteCount)
                {
                    highestViablePriority = nodePriorityList;
                    break;
                }
            }

            if (highestViablePriority != -1)
                foreach (NodeData endingNode in nodeDict[highestViablePriority])
                    newPaths.Add(GeneratePathInfo(endingNode, source.sourceObject, target.sourceObject));

            return (newPaths);
        }

        public void DrawPaths(List<PathInfo> primaryPaths, List<PathInfo> secondaryPaths)
        {
            foreach (PathInfo path in primaryPaths)
                DrawPathInfo(path, primaryPaths, secondaryPaths);
        }
        public void DrawPathInfo(PathInfo pathInfo, List<PathInfo> primaryPaths, List<PathInfo> secondaryPaths)
        {
            if (pathInfo.nodes.Count == 0) return;
            Evaluation pathEvaluation = new Evaluation(pathInfo, primaryPaths, secondaryPaths, nodeEvaluationTypeToggle);

            if (pathInfo.pathSource != null)
                Gizmos.DrawLine(pathInfo.pathSource.transform.position, pathInfo.nodes.First().nodeObject.transform.position);

            foreach (NodeInfo nodeInfo in pathInfo.nodes)
            {
                int nodeScore = pathEvaluation.nodeScores[nodeInfo];
                Gizmos.color = colorCollection.GetColor(pathEvaluation.nodeContextualScores.Keys.ToList().IndexOf(nodeInfo));
                Vector3 gizmosPosition = nodeInfo.nodeObject.transform.position;
                //Handles.Label(gizmosPosition, nodeScore.ToString());
                Gizmos.DrawSphere(gizmosPosition, sphereRadius);
                if (nodeInfo != pathInfo.nodes.Last())
                    Gizmos.DrawLine(gizmosPosition, pathInfo.nodes[pathInfo.nodes.IndexOf(nodeInfo) + 1].nodeObject.transform.position);
            }

            //if (pathInfo.pathTarget != null)
            //Gizmos.DrawLine(pathInfo.pathTarget.transform.position, pathInfo.nodes.Last().nodeObject.transform.position);
        }

        public PathInfo GeneratePathInfo(NodeData nodeData, GameObject sourceObject, GameObject targetObject = null)
        {
            List<NodeData> collectedNodes = new List<NodeData>();
            RecursiveAddNodeParentToList(collectedNodes, nodeData);

            List<NodeInfo> allNodeInfos = new List<NodeInfo>();

            for (int i = collectedNodes.Count - 1; i > -1; i--)
                allNodeInfos.Add(nodeInfoObjectDict[collectedNodes[i].node]);
            PathInfo newPathInfo = new PathInfo(allNodeInfos, sourceObject, targetObject);
            return (newPathInfo);
        }

        public void RecursiveAddNodeParentToList(List<NodeData> nodeList, NodeData nodeData)
        {
            nodeList.Add(nodeData);
            if (nodeData.parentNode != null)
                RecursiveAddNodeParentToList(nodeList, nodeData.parentNode);
        }

        public List<NodeData> GetNodesFromObject(GameObject compareObject, List<GameObject> nodes, int priority, float distance)
        {
            List<NodeData> returnList = new List<NodeData>();
            foreach (GameObject aiNode in nodes)
                if (Vector3.Distance(aiNode.transform.position, compareObject.transform.position) < distance)
                    returnList.Add(new NodeData(aiNode, priority, compareObject));

            return (returnList);
        }

        public static Dictionary<NodeInfo, int> GetOverlapCount(List<PathInfo> primaryPaths, List<PathInfo> secondaryPaths)
        {
            Dictionary<NodeInfo, int> returnDict = new Dictionary<NodeInfo, int>();
            List<NodeInfo> primaryPathNodes = UnpackPathInfos(primaryPaths);

            foreach (NodeInfo secondaryPathNode in UnpackPathInfos(secondaryPaths))
                if (primaryPathNodes.Contains(secondaryPathNode))
                {
                    if (returnDict.ContainsKey(secondaryPathNode))
                        returnDict[secondaryPathNode]++;
                    else
                        returnDict.Add(secondaryPathNode, 1);
                }

            return (returnDict);
        }

        public static List<NodeInfo> UnpackPathInfos(List<PathInfo> pathInfos)
        {
            List<NodeInfo> returnList = new List<NodeInfo>();
            foreach (PathInfo pathInfo in pathInfos)
                foreach (NodeInfo nodeInfo in pathInfo.nodes)
                    returnList.Add(nodeInfo);
            return (returnList);
        }
    }
}
