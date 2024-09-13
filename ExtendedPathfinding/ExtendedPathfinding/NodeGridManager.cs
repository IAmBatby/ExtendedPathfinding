using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    public enum NodeDrawType { Draw, Void, Cull };
    [ExecuteAlways]
    public class NodeGridManager : MonoBehaviour
    {
        public Vector3 spawnRange;
        [HideInInspector] public Vector3 startingPosition;
        //private Vector3 arrayCenterOffset => new Vector3((float)spawnRange.x / 2, (float)spawnRange.y / 2, (float)spawnRange.z / 2);
        //private Vector3 arrayCenterPosition => (Vector3.zero - arrayCenterOffset) + (transform.lossyScale / 2);

        public List<GameObject> AINodes = new List<GameObject>();
        [HideInInspector] public MeshFilter meshFilter;
        [HideInInspector] public enum ScaleMode { Scale, Bounds }
        public ScaleMode scaleMode;

        public static int WalkableArea = -111;
        public LayerMask hitMask;
        public HeatManifest heatManifest;


        [Range(0, 500000)]
        public int maxNodes = 50000;

        [HideInInspector] public NodeGridInfo[,,] nodeGridMatrix;

        [HideInInspector] public NodeGridInfo[] nodeGridInfos;

        [HideInInspector] public NodeGridInfo[][] allNodeGrids;

        [HideInInspector] public List<DrawCubeInfo>[,] drawCubes;

        public DrawGridInfo DrawInfo;
        public DrawGridInfo VoidInfo;
        public DrawGridInfo CullInfo;


        public Vector3 gridScale;
        public float navMeshSampleDistance;
        public bool rebakeButtonBool;
        public bool bakeAlways;

        public bool debugSplitPoints;


        private void Start()
        {

        }

        private void FixedUpdate()
        {
        }

        private void Update()
        {
            if (rebakeButtonBool == true || bakeAlways == true)
            {
                BakeGridData();
                rebakeButtonBool = false;
            }
        }

        private void BakeGridData()
        {
            AINodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
            PopulateNodeGridMatrix();
            PopulateDrawCubeInfo();
        }

        private void OnDrawGizmos()
        {

            Gizmos.matrix = transform.localToWorldMatrix;

            DrawBoundries();
            if (enabled == false) return;
            if (nodeGridMatrix == null) return;
            if (nodeGridInfos == null) return;
            if (drawCubes == null) return;
            if (WalkableArea == -1)
                WalkableArea = 1 << NavMesh.GetAreaFromName("Walkable");
            //DrawCubeInfoLists();
            DrawNodeGridInfos();

        }

        private Vector3 GetStartingPosition()
        {
            Vector3 value = (transform.position - transform.lossyScale / 2) + (gridScale / 2);
            value -= transform.lossyScale / 2;
            return (value);
        }

        public void DrawCubeInfoLists()
        {
            int drawCount = 0;
            for (int z = 0; z < drawCubes.GetLength(0); z++)
            {
                for (int x = 0; x < drawCubes.GetLength(1); x++)
                {
                    if (DrawGridCubeList(drawCubes[x, z]) == true)
                        drawCount += drawCubes[x, z].Count;
                    if (drawCount >= maxNodes)
                    {
                        Debug.Log("Drew: " + drawCount + " Cubes, Exiting Early");
                        return;
                    }    
                }
            }
            Debug.Log("Drew: " + drawCount + " Cubes, Finished All");
        }

        public void DrawNodeGridInfos()
        {
            int drawCount = 0;
            for (int i = 0; i < nodeGridInfos.Length; i++)
            {
                if (DrawGridCube(nodeGridInfos[i]))
                    drawCount++;
                if (drawCount > maxNodes)
                {
                    Debug.Log("Drew: " + drawCount + " Cubes, Exiting Early");
                    return;
                }
            }
            Debug.Log("Drew: " + drawCount + " Cubes, Finished All");
        }

        private void PopulateNodeGridMatrix()
        {
            Vector3Int spawnRangeAmount = new Vector3Int((int)spawnRange.x, (int)spawnRange.y, (int) spawnRange.z);
            nodeGridInfos = new NodeGridInfo[spawnRangeAmount.x * spawnRangeAmount.y * spawnRangeAmount.z];
            nodeGridMatrix = new NodeGridInfo[spawnRangeAmount.x, spawnRangeAmount.y, spawnRangeAmount.z];

            Vector3 startingPostion = GetStartingPosition();

            int count = 0;
            NodeGridInfo previousNode = default;
            for (int z = 0; z < spawnRange.z; z++)
            {
                for (int x = 0; x < spawnRange.x; x++)
                {
                    previousNode.drawType = VoidInfo;
                    for (int y = 0; y < spawnRange.y; y++)
                    {
                        Vector3 adjustedPosition = startingPostion + new Vector3(gridScale.x * x, gridScale.y * y, gridScale.z * z);
                        bool validSample = ValidatePosition(adjustedPosition);
                        DrawGridInfo newDrawType;

                        if (validSample == true && previousNode.drawType != VoidInfo)
                            newDrawType = CullInfo;
                        else if (validSample == true)
                            newDrawType = new DrawGridInfo(DrawInfo.type, GetNodeDistanceColor(transform.position + adjustedPosition), DrawInfo.shouldDraw);
                        else
                            newDrawType = VoidInfo;

                        NodeGridInfo newNodeGridInfo = new NodeGridInfo(adjustedPosition, new Vector3Int(x, y, z), newDrawType);

                        if (previousNode.drawType == CullInfo && ((newNodeGridInfo.drawType == VoidInfo || y == spawnRange.y)))
                        {
                            NodeGridInfo oldInfo = new NodeGridInfo(previousNode.position, previousNode.index, DrawInfo);
                            nodeGridInfos[count - 1] = oldInfo;
                            nodeGridMatrix[x, y - 1, z] = oldInfo;
                        }

                        nodeGridMatrix[x, y, z] = newNodeGridInfo;
                        nodeGridInfos[count] = newNodeGridInfo;
                        previousNode = newNodeGridInfo;
                        count++;
                    }
                }
            }
        }

        public Color GetNodeDistanceColor(Vector3 position)
        {
            GameObject closestNode = null;
            float closestNodeDistance = -1;

            foreach (GameObject AINodeObject in AINodes)
            {
                float newDistance = Vector3.Distance(position, AINodeObject.transform.position);
                if (closestNode == null || newDistance < closestNodeDistance)
                {
                    closestNode = AINodeObject;
                    closestNodeDistance = newDistance;
                }
            }

            return (heatManifest.GetThresholdColor(closestNodeDistance));
        }

        private void PopulateDrawCubeInfo()
        {
            Vector3Int spawnRangeAmount = new Vector3Int((int)spawnRange.x, (int)spawnRange.y, (int)spawnRange.z);
            drawCubes = new List<DrawCubeInfo>[spawnRangeAmount.x, spawnRangeAmount.z];

            for (int z = 0; z < spawnRange.z; z++)
            {
                for (int x = 0; x < spawnRange.x; x++)
                {
                    List<DrawCubeInfo> newDrawCubeList = new List<DrawCubeInfo>();
                    DrawGridInfo currentInfo = null;
                    List<NodeGridInfo> collectedNodes = new List<NodeGridInfo>();
                    for (int y = 0; y < spawnRange.y; y++)
                    {
                        NodeGridInfo nodeGridInfo = nodeGridMatrix[x, y, z];
                        if (currentInfo != null && currentInfo != nodeGridInfo.drawType)
                        {
                            newDrawCubeList.Add(CreateNewDrawCubeInfo(collectedNodes, currentInfo));
                            collectedNodes.Clear();
                        }
                        collectedNodes.Add(nodeGridInfo);
                        currentInfo = nodeGridInfo.drawType;
                    }
                    if (collectedNodes.Count > 0)
                        newDrawCubeList.Add(CreateNewDrawCubeInfo(collectedNodes, collectedNodes[0].drawType));
                    drawCubes[x,z] = new List<DrawCubeInfo>(newDrawCubeList);
                }
            }

            Debug.Log(drawCubes.Length);
        }

        private DrawCubeInfo CreateNewDrawCubeInfo(List<NodeGridInfo> allNodes, DrawGridInfo info)
        {
            if (allNodes.Count == 1)
                return (new DrawCubeInfo(allNodes[0].position, gridScale, info));
            else
            {
                Vector3 centerPosition = Vector3.Lerp(allNodes.First().position, allNodes.Last().position, 0.5f);
                Vector3 scale = new Vector3(gridScale.x, gridScale.y * allNodes.Count, gridScale.z);
                return (new DrawCubeInfo(centerPosition, scale, info));
            }
        }

        private bool DrawGridCube(NodeGridInfo nodeGridInfo)
        {
            if (nodeGridInfo.drawType.shouldDraw == false) return (false);

            Gizmos.color = nodeGridInfo.drawType.color;
            Gizmos.DrawCube(nodeGridInfo.position, gridScale);
            return (true);
        }

        private bool DrawGridCubeList(List<DrawCubeInfo> cubeList)
        {
            if (cubeList == null || cubeList.Count == 0) return (false);
            if (cubeList[0].drawType.shouldDraw == false) return (false);

            Gizmos.color = cubeList[0].drawType.color;
            for (int i = 0; i < cubeList.Count; i++)
            {
                if (i == cubeList.Count - 1 && debugSplitPoints == true)
                    Gizmos.color = Color.white;
                Gizmos.DrawCube(cubeList[i].position, cubeList[i].scale);
                if (i == cubeList.Count - 1 && debugSplitPoints == true)
                    Gizmos.color = cubeList[0].drawType.color;
            }
            return (true);
        }

        private bool ValidatePosition(Vector3 position)
        {
            if (Physics.Raycast(transform.position + position, Vector3.down, out RaycastHit raycastHit, Mathf.Infinity, layerMask: hitMask))
                if (NavMesh.SamplePosition(raycastHit.point, out NavMeshHit hit, maxDistance: navMeshSampleDistance, WalkableArea))
                    return (true);
            return (false);
        }

        public void DrawBoundries()
        {
            Vector3 startingPostion = GetStartingPosition();
            for (int z = 0; z < spawnRange.z; z++)
            {
                for (int x = 0; x < spawnRange.x; x++)
                {
                    for (int y = 0; y < spawnRange.y; y++)
                    {
                        Vector3 adjustedPosition = startingPostion + new Vector3(gridScale.x * x, gridScale.y * y, gridScale.z * z);

                        if (Check(z, (int)spawnRange.z, x, (int)spawnRange.x, y, (int)spawnRange.y))
                            Gizmos.DrawCube(adjustedPosition, gridScale);
                    }
                }
            }
        }

        public bool Check(int x, int xMax, int z, int zMax, int y, int yMax)
        {
            int count = 0;

            if (x == 0) count++;
            if (x == xMax - 1) count++;
            if (z == 0) count++;
            if (z == zMax - 1) count++;
            if (y == 0) count++;
            if (y == yMax - 1) count++;

            if (count >= 2)
                return (true);
            return (false);
        }
    }

    public struct DrawCubeInfo
    {
        public Vector3 position;
        public Vector3 scale;
        public DrawGridInfo drawType;

        public DrawCubeInfo(Vector3 newPosition, Vector3 newScale, DrawGridInfo newDrawType)
        {
            position = newPosition;
            scale = newScale;
            drawType = newDrawType;
        }
    }

    public struct NodeGridInfo
    {
        public Vector3 position;
        public Vector3Int index;
        public DrawGridInfo drawType;

        public NodeGridInfo(Vector3 newPosition, Vector3Int newIndex, DrawGridInfo newDrawType)
        {
            position = newPosition;
            index = newIndex;
            drawType = newDrawType;
        }
    }

    [System.Serializable]
    public class DrawGridInfo
    {
        public NodeDrawType type;
        public Color color;
        public bool shouldDraw;

        public DrawGridInfo(NodeDrawType newType, Color newColor, bool newShouldDraw)
        {
            type = newType;
            color = newColor;
            shouldDraw = newShouldDraw;
        }
    }

}
