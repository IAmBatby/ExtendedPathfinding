using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    public class NodeManager : MonoBehaviour
    {
        [Header("Sources")]
        public NodeSource shipSource;
        public NodeSource entranceSource;

        [System.Flags]
        public enum PathView
        {
            None = 0,
            Entrance = 1 << 0,
            Ship = 1 << 1,
            Connections = 1 << 2,
            Unused = 1 << 3
        }

        [Space(10)]
        [Header("Debug Views")]

        public PathView pathViewToggle;

        public enum NodeValueType { Depth, Usage, Heat, Distance }
        public NodeValueType nodeValueType;

        [Space(10)]
        [Header("Settings")]
        [Range(-1, 100)]
        public int selectedPathsViewRangedIndex;
        [Space(5)]
        [Range(1, 10)]
        public int iterationCount;
        public int targetRouteCount;
        public int overlappingNodeCount;
        public int highestOverlapLeewayOffset;
        public List<Color> gizmosColors = new List<Color>();
        public List<int> hotspotBlacklist = new List<int>();
        public int trimThreshold;
        public float sphereSize;

        [Space(10)]
        [Header("Debug")]
        public NodePath connectedPath;

        [HideInInspector] public List<GameObject> allAINodes;

        List<NodePath> selectedNodePaths = new List<NodePath>();

        List<NodePath> shipBranchPaths;
        List<NodePath> entranceBranchPaths;
        List<NodePath> connectedPaths;

        List<NodePath> AllFoundPaths
        {
            get
            {
                List<NodePath> returnList = new List<NodePath>();
                if (shipBranchPaths != null)
                    returnList.AddRange(shipBranchPaths);
                if (entranceBranchPaths != null)
                    returnList.AddRange(entranceBranchPaths);
                if (connectedPaths != null)
                    returnList.AddRange(connectedPaths);
                return (returnList);
            }
        }

        Dictionary<NodeData, int> nodeActivityDictionary;
        Dictionary<GameObject, int> nodeHotspotDictionary;

        Dictionary<NodePath, float> branchDistanceDictionary;

        public void Start()
        {

        }

        public void OnDrawGizmos()
        {
            allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();


            if (isActiveAndEnabled)
            {
                RefreshPaths();
                SelectPaths();
                SortPaths();
                DrawPaths();
            }
        }

        public void RefreshPaths()
        {
            shipBranchPaths = GetNodePaths(GenerateNodeData(shipSource), targetRouteCount);
            entranceBranchPaths = GetNodePaths(GenerateNodeData(entranceSource), targetRouteCount);
            connectedPaths = new List<NodePath>();

            int highestOverlapCount = 0;

            foreach (NodePath shipNodePath in shipBranchPaths)
                foreach (NodePath entranceNodePath in entranceBranchPaths)
                {
                    int overlapCount = ComparePaths(shipNodePath, entranceNodePath);
                    if (overlapCount > highestOverlapCount)
                        highestOverlapCount = overlapCount;
                }

            foreach (NodePath shipNodePath in shipBranchPaths)
                foreach (NodePath entranceNodePath in entranceBranchPaths)
                {
                    int overlapCount = ComparePaths(shipNodePath, entranceNodePath);
                    if (overlapCount == highestOverlapCount || overlapCount == highestOverlapCount - highestOverlapLeewayOffset)
                    {
                        NodePath newConnectedPath = ConnectPaths(shipNodePath, entranceNodePath);
                        if (IsUniquePath(connectedPaths, newConnectedPath))
                            connectedPaths.Add(newConnectedPath);
                    }
                }


            nodeActivityDictionary = new Dictionary<NodeData, int>();

            foreach (NodePath selectedPath in new List<NodePath>(shipBranchPaths).Concat(entranceBranchPaths).Concat(connectedPaths).ToList())
                foreach (NodeData selectedNode in selectedPath.nodes)
                {
                    if (nodeActivityDictionary.ContainsKey(selectedNode))
                        nodeActivityDictionary[selectedNode]++;
                    else
                        nodeActivityDictionary.Add(selectedNode, 1);
                }

            nodeHotspotDictionary = GetHotspotData(shipBranchPaths, entranceBranchPaths);

            branchDistanceDictionary = GetDistanceData();
        }

        public void SelectPaths()
        {
            selectedNodePaths.Clear();

            if (pathViewToggle.HasFlag(PathView.Ship))
                selectedNodePaths.AddRange(shipBranchPaths);
            if (pathViewToggle.HasFlag(PathView.Entrance))
                selectedNodePaths.AddRange(entranceBranchPaths);
            if (pathViewToggle.HasFlag(PathView.Connections))
                selectedNodePaths.AddRange(connectedPaths);
        }

        public void SortPaths()
        {
            if (nodeValueType == NodeValueType.Distance)
            {
                Dictionary<NodePath, float> tempDict = new Dictionary<NodePath, float>(branchDistanceDictionary);
                List<NodePath> unsortedNodePaths = new List<NodePath>(selectedNodePaths);
                selectedNodePaths.Clear();
                foreach (NodePath selectedPath in unsortedNodePaths)
                    if (!tempDict.ContainsKey(selectedPath))
                        tempDict.Add(selectedPath, Mathf.Infinity);
                foreach (NodePath sortedNodePath in tempDict.OrderBy(kvp => kvp.Value).Select(k => k.Key))
                    if (unsortedNodePaths.Contains(sortedNodePath))
                        selectedNodePaths.Add(sortedNodePath);
            }

        }

        public void DrawPaths()
        {
            List<GameObject> selectedObjects = new List<GameObject>();
            foreach (NodePath selectedPath in AllFoundPaths)
                foreach (NodeData selectedNode in selectedPath.nodes)
                    if (!selectedObjects.Contains(selectedNode.node))
                        selectedObjects.Add(selectedNode.node);

            if (selectedPathsViewRangedIndex == -1)
            {
                foreach (NodePath selectedPath in selectedNodePaths)
                    DrawNodePath(selectedPath);
            }
            else
                DrawNodePath(selectedNodePaths[Mathf.RoundToInt(Mathf.Lerp(0f, (float)(selectedNodePaths.Count - 1), (float)selectedPathsViewRangedIndex / 100f))]);


            //Draw Unused Paths
            if (pathViewToggle.HasFlag(PathView.Unused))
            {
                Gizmos.color = Color.white;
                foreach (GameObject orphanedObject in allAINodes)
                    if (!selectedObjects.Contains(orphanedObject))
                        Gizmos.DrawSphere(orphanedObject.transform.position + new Vector3(0, 50f, 0), sphereSize);
            }
        }

        public Dictionary<GameObject, int> GetHotspotData(List<NodePath> firstPaths, List<NodePath> secondPaths)
        {
            Dictionary<GameObject, int> returnDict = new Dictionary<GameObject, int>();

            List<GameObject> firstPathsNodeObjects = new List<GameObject>();
            foreach (NodePath firstBranchPath in firstPaths)
                foreach (NodeData firstBranchNode in firstBranchPath.nodes)
                    if (!firstPathsNodeObjects.Contains(firstBranchNode.node))
                        firstPathsNodeObjects.Add(firstBranchNode.node);

            List<GameObject> secondPathsNodeObjects = new List<GameObject>();
            foreach (NodePath secondBranchPath in secondPaths)
                foreach (NodeData secondBranchNode in secondBranchPath.nodes)
                    if (!secondPathsNodeObjects.Contains(secondBranchNode.node))
                        secondPathsNodeObjects.Add(secondBranchNode.node);

            foreach (NodePath secondPath in secondPaths)
                foreach (NodeData secondBranchNode in secondPath.nodes)
                    if (firstPathsNodeObjects.Contains(secondBranchNode.node))
                    {
                        if (returnDict.TryGetValue(secondBranchNode.node, out int hotspotValue))
                            returnDict[secondBranchNode.node]++;
                        else
                            returnDict.Add(secondBranchNode.node, 1);
                    }

            foreach (NodePath firstPath in firstPaths)
                foreach (NodeData firstBranchNode in firstPath.nodes)
                    if (secondPathsNodeObjects.Contains(firstBranchNode.node))
                    {
                        if (returnDict.TryGetValue(firstBranchNode.node, out int hotspotValue))
                            returnDict[firstBranchNode.node]++;
                        else
                            returnDict.Add(firstBranchNode.node, 1);
                    }

            return (returnDict);
        }

        public Dictionary<NodePath, float> GetDistanceData()
        {
            Dictionary<NodePath, float> returnDict = new Dictionary<NodePath, float>();

            foreach (NodePath firstBranchPath in shipBranchPaths)
            {
                List<float> distances = new List<float>();
                foreach (NodeData branchNode in firstBranchPath.nodes)
                    distances.Add(Vector3.Distance(branchNode.node.transform.position, entranceSource.sourceObject.transform.position));
                returnDict.Add(firstBranchPath, distances.Average() * firstBranchPath.nodes.Count);
            }

            foreach (NodePath secondBranchPath in entranceBranchPaths)
            {
                List<float> distances = new List<float>();
                foreach (NodeData branchNode in secondBranchPath.nodes)
                    distances.Add(Vector3.Distance(branchNode.node.transform.position, shipSource.sourceObject.transform.position));
                returnDict.Add(secondBranchPath, distances.Average() * secondBranchPath.nodes.Count);
            }

            return (returnDict);

        }

        public bool IsUniquePath(List<NodePath> paths, NodePath newPath)
        {
            foreach (NodePath path in paths)
                if (ComparePaths(path, newPath) == newPath.nodes.Count)
                    return (false);

            return (true);
        }

        public void TrimPath(NodePath path)
        {
            /*
            foreach (NodeData node in new List<NodeData>(path.nodes))
                if (nodeHotspotDict.TryGetValue(node, out int hotspotValue))
                    if (hotspotValue < trimThreshold)
            */
        }

        public int ComparePaths(NodePath firstPath, NodePath secondPath)
        {
            int overlapCount = 0;
            List<GameObject> firstPathNodeObjects = firstPath.nodes.Select(n => n.node).ToList();
            List<GameObject> secondPathNodeObjects = secondPath.nodes.Select(n => n.node).ToList();
            foreach (GameObject firstPathObject in firstPathNodeObjects)
                if (secondPathNodeObjects.Contains(firstPathObject))
                    overlapCount++;

            return (overlapCount);
        }

        public NodePath ConnectPaths(NodePath firstNodePath, NodePath secondNodePath)
        {
            NodePath connectedPath = null;
            int overlapIndex = 0;

            List<NodeData> selectedPathNodes = new List<NodeData>();
            List<NodeData> tempNewPathNodes = new List<NodeData>();
            List<NodeData> newPathNodes = new List<NodeData>();

            List<GameObject> firstPathNodeObjects = firstNodePath.nodes.Select(n => n.node).ToList();
            List<GameObject> secondPathNodeObjects = secondNodePath.nodes.Select(n => n.node).ToList();
            foreach (GameObject secondPathObject in secondPathNodeObjects)
                if (firstPathNodeObjects.Contains(secondPathObject))
                    overlapIndex = secondPathNodeObjects.IndexOf(secondPathObject);

            for (int i = 0; i < secondNodePath.nodes.Count; i++)
                if (i > overlapIndex)
                    selectedPathNodes.Add(secondNodePath.nodes[i]);

            for (int i = 0; i < firstNodePath.nodes.Count; i++)
                if (i <= overlapIndex)
                    selectedPathNodes.Add(firstNodePath.nodes[i]);


            //NodeData firstSelectedNode = selectedPathNodes[0];
            //selectedPathNodes.Remove(firstSelectedNode);
            //selectedPathNodes.Add(firstSelectedNode);

            for (int i = 0; i < selectedPathNodes.Count; i++)
            {
                if (i == 0)
                    tempNewPathNodes.Add(new NodeData(selectedPathNodes[i].node, i + 1, null));
                else
                {
                    NodeData newNodeData = new NodeData(selectedPathNodes[i].node, i + 1, null);
                    tempNewPathNodes.Add(newNodeData);
                }
            }

            int counter = 0;
            foreach (NodeData nodeData in tempNewPathNodes)
            {
                counter++;
                if (counter < tempNewPathNodes.Count)
                {
                    nodeData.parentNode = tempNewPathNodes[counter];
                    nodeData.parents[0] = tempNewPathNodes[counter].node;
                }
            }

            //for (int i = tempNewPathNodes.Count - 1; i > -1; i--)
            //newPathNodes.Add(tempNewPathNodes[i]);

            for (int i = 0; i < tempNewPathNodes.Count; i++)
                tempNewPathNodes[i].nodePriority = tempNewPathNodes.Count - i;

            tempNewPathNodes.Last().parents[0] = firstNodePath.nodes.Last().parents[0];

            //newPathNodes[0].parents[0] = firstNodePath.nodes.Last().parents[0];

            connectedPath = new NodePath(tempNewPathNodes);

            return (connectedPath);
        }

        public List<NodePath> GetNodePaths(Dictionary<int, List<NodeData>> nodesDict, int targetRouteCount)
        {
            List<NodePath> nodePaths = new List<NodePath>();

            int highestViablePriority = -1;
            for (int nodePriorityList = nodesDict.Count - 1; nodePriorityList > -1; nodePriorityList--)
            {
                if (nodesDict.ContainsKey(nodePriorityList) && nodesDict[nodePriorityList].Count >= targetRouteCount)
                {
                    highestViablePriority = nodePriorityList;
                    break;
                }
            }

            if (highestViablePriority != -1)
                foreach (NodeData endingNode in nodesDict[highestViablePriority])
                    nodePaths.Add(GenerateNodePath(endingNode));

            return (nodePaths);
        }

        public NodePath GenerateNodePath(NodeData highestDepthNodeChild)
        {
            NodePath newNodePath = new NodePath();
            AddNodeToPathAndTryAddParent(newNodePath, highestDepthNodeChild);

            return (newNodePath);
        }

        public void AddNodeToPathAndTryAddParent(NodePath nodePath, NodeData nodeData)
        {
            nodePath.nodes.Add(nodeData);
            if (nodeData.parentNode != null)
                AddNodeToPathAndTryAddParent(nodePath, nodeData.parentNode);
        }

        public Dictionary<int, List<NodeData>> GenerateNodeData(NodeSource nodeSource)
        {
            return (GenerateNodeData(nodeSource.sourceObject, nodeSource.distanceRange));
        }

        public Dictionary<int, List<NodeData>> GenerateNodeData(GameObject sourceObject, float distance)
        {
            List<NodeData> newNodeDataList = new List<NodeData>();
            Dictionary<int, List<NodeData>> nodeDict = new Dictionary<int, List<NodeData>>();

            List<GameObject> temporaryNodes = new List<GameObject>(allAINodes);

            foreach (NodeData priorityOneNode in GetNodesFromObject(sourceObject, new List<GameObject>(temporaryNodes), 1, distance))
            {
                temporaryNodes.Remove(priorityOneNode.node);
                AddToDictionary(nodeDict, priorityOneNode);
            }

            for (int priority = 2; priority < iterationCount + 2; priority++)
            {
                if (nodeDict.ContainsKey(priority - 1))
                {
                    foreach (NodeData previousNodeData in nodeDict[priority - 1])
                    {
                        foreach (NodeData priorityNode in GetNodesFromObject(previousNodeData.node, new List<GameObject>(temporaryNodes), priority, distance))
                        {
                            priorityNode.parentNode = previousNodeData;
                            temporaryNodes.Remove(priorityNode.node);
                            AddToDictionary(nodeDict, priorityNode);
                        }
                    }
                }
            }

            return (nodeDict);
        }

        public void AddToDictionary(Dictionary<int, List<NodeData>> nodeDict, NodeData newNode)
        {
            if (nodeDict.TryGetValue(newNode.nodePriority, out List<NodeData> nodeDictValues))
                nodeDictValues.Add(newNode);
            else
                nodeDict.Add(newNode.nodePriority, new List<NodeData> { newNode });
        }

        public List<NodeData> GetNodesFromObject(GameObject compareObject, List<GameObject> nodes, int priority, float distance)
        {
            List<NodeData> returnList = new List<NodeData>();
            foreach (GameObject aiNode in nodes)
                if (TryCreateNodeData(aiNode, compareObject, priority, distance, out NodeData newNodeData))
                    returnList.Add(newNodeData);

            return (returnList);
        }

        public bool TryCreateNodeData(GameObject firstObject, GameObject secondObject, int priority, float distance, out NodeData newNodeData)
        {
            newNodeData = null;

            if (Vector3.Distance(firstObject.transform.position, secondObject.transform.position) < distance)
                newNodeData = new NodeData(firstObject, priority, secondObject);

            return (newNodeData != null);
        }

        public void SelectNodePath(NodePath nodePath)
        {
            selectedNodePaths.Add(nodePath);
        }

        public void DrawNodePath(NodePath nodePath)
        {
            DrawNodeAndTryDrawNodeParent(nodePath.nodes[0], nodePath);
        }

        public void DrawNodeAndTryDrawNodeParent(NodeData nodeData, NodePath nodePath)
        {
            DrawNode(nodeData, nodePath);
            if (nodeData.parentNode != null)
                DrawNodeAndTryDrawNodeParent(nodeData.parentNode, nodePath);
        }

        public void DrawNode(NodeData nodeData, NodePath nodePath)
        {
            Vector3 positionOffset = new Vector3(0, 50f, 0);
            Color drawColor = Color.white;
            int nodeValue = -1;
            string nodeDisplayValue = string.Empty;

            if (nodeValueType == NodeValueType.Depth)
                nodeValue = nodeData.nodePriority;
            else if (nodeValueType == NodeValueType.Usage)
                nodeValue = nodeActivityDictionary[nodeData];
            else if (nodeValueType == NodeValueType.Heat)
            {
                if (nodeHotspotDictionary.ContainsKey(nodeData.node))
                    nodeValue = nodeHotspotDictionary[nodeData.node];
                else
                    nodeDisplayValue = "0";
            }
            else if (nodeValueType == NodeValueType.Distance)
            {
                if (branchDistanceDictionary.ContainsKey(nodePath))
                {
                    drawColor = Color.white;
                    nodeDisplayValue = Mathf.FloorToInt(branchDistanceDictionary[nodePath]).ToString();
                }
            }

            if (nodeValue != -1)
            {
                if (nodeValue < gizmosColors.Count)
                    drawColor = gizmosColors[nodeValue - 1];
                else
                    drawColor = gizmosColors.Last();
            }
            else
                drawColor = Color.black;

            if (nodeValue != -1)
                nodeDisplayValue = nodeValue.ToString();

            Gizmos.color = drawColor;

            //Handles.Label(nodeData.node.transform.position + positionOffset, nodeDisplayValue);

            if (nodeData.parents[0] != null)
                Gizmos.DrawLine(nodeData.node.transform.position + positionOffset, nodeData.parents[0].transform.position + positionOffset);

            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a / 2);
            Gizmos.DrawSphere(nodeData.node.transform.position + positionOffset, sphereSize);
        }
    }

    [System.Serializable]
    public class NodeData
    {
        public GameObject node;

        [SerializeReference] public NodeData parentNode;

        public int nodePriority;

        public List<GameObject> parents = new List<GameObject>();

        public NodeData(GameObject newNode, int newPriority, GameObject parent)
        {
            node = newNode;
            nodePriority = newPriority;
            parents.Add(parent);
        }
    }

    [System.Serializable]
    public class NodePath
    {
        public List<NodeData> nodes = new List<NodeData>();

        public NodePath(List<NodeData> newNodes = null)
        {
            if (newNodes != null)
                nodes = newNodes;
        }
    }

    [System.Serializable]
    public class NodeSource
    {
        public GameObject sourceObject;
        public float distanceRange;
    }
}
