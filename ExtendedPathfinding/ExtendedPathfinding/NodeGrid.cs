using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    [ExecuteAlways]
    public class NodeGrid : MonoBehaviour
    {
        public Vector3 spawnRange;
        public Vector3 startingPosition;
        //private Vector3 arrayCenterOffset => new Vector3((float)spawnRange.x / 2, (float)spawnRange.y / 2, (float)spawnRange.z / 2);
        //private Vector3 arrayCenterPosition => (Vector3.zero - arrayCenterOffset) + (transform.lossyScale / 2);

        public MeshFilter meshFilter;
        public enum ScaleMode { Scale, Bounds }
        public ScaleMode scaleMode;

        public static int WalkableArea;


        [Range(0, 100000)]
        public float maxCubes;

        public Vector3[,,] nodeGridMatrix;

        public Color Invisible = new Color(0, 0, 0, 0);

        public Vector3 gridScale;
        public float navMeshSampleDistance;
        public bool drawInvisible;
        public bool rebakeButtonBool;
        public bool bakeAlways;
        public bool useMatrixData;

        private void Start()
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
            PopulateNodeGridMatrix();
        }

        private void OnDrawGizmos()
        {
            if (meshFilter == null) return;
            if (enabled == false) return;
            if (nodeGridMatrix == null) return;
            WalkableArea = 1 << NavMesh.GetAreaFromName("Walkable");

            Gizmos.matrix = transform.localToWorldMatrix;
            GetUnits();

        }

        private Vector3 GetStartingPosition()
        {
            Vector3 value = (transform.position - transform.lossyScale / 2) + (gridScale / 2);
            value -= transform.lossyScale / 2;
            return (value);
        }

        public void GetUnits()
        {
            int drawCount = 0;
            for (int y = 0; y < spawnRange.y; y++)
            {
                for (int z = 0; z < spawnRange.z; z++)
                {
                    for (int x = 0; x < spawnRange.x; x++)
                    {
                        Vector3 nodePosition = GetStartingPosition() + nodeGridMatrix[x,y,z];
                        Color nodeColor = ValidatePosition(nodePosition);
                        DrawGridCube(nodePosition, nodeColor);
                        drawCount++;
                        if (drawCount > maxCubes) return;
                    }
                }
            }
        }

        private void PopulateNodeGridMatrix()
        {
            nodeGridMatrix = new Vector3[(int)spawnRange.x, (int)spawnRange.y, (int)spawnRange.z];

            for (int y = 0; y < spawnRange.y; y++)
            {
                for (int z = 0; z < spawnRange.z; z++)
                {
                    for (int x = 0; x < spawnRange.x; x++)
                    {
                        Vector3 adjustedPosition = new Vector3(gridScale.x * x, gridScale.y * y, gridScale.z * z);
                        nodeGridMatrix[x, y, z] = adjustedPosition;
                    }
                }
            }
        }

        private void DrawGridCube(Vector3 position, Color color)
        {
            if (color == Invisible && drawInvisible == false) return;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
            Gizmos.DrawCube(position, gridScale);
        }

        private Color ValidatePosition(Vector3 position)
        {
            if (NavMesh.SamplePosition(transform.position + position, out NavMeshHit hit, maxDistance: navMeshSampleDistance, WalkableArea))
                return (Color.green);
            else
                return (Invisible);
        }
    }
}
