using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace ExtendedPathfinding.ExtendedPathfinding
{
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

        public float navMeshSampleDistance;

        [Range(0, 100000)]
        public float maxCubes;

        public Vector3 gridScale;

        bool drawInvisible;

        public Color Invisible = new Color(0, 0, 0, 0);

        private void Start()
        {

        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 100, Screen.height - 100, 100, 50), new GUIStyle());
            if (GUILayout.Button("Rebake Grid"))
                BakeGridData();
        }

        private void BakeGridData()
        {

        }

        private void OnDrawGizmos()
        {
            if (meshFilter == null) return;
            if (enabled == false) return;
            WalkableArea = 1 << NavMesh.GetAreaFromName("Walkable");

            //startingPosition = (meshFilter.transform.position - meshFilter.transform.lossyScale) + (gridScale / 2) - meshFilter.sharedMesh.bounds.min;

            //DrawGridCube(startingPosition);

            //startingPosition.x += gridScale.x;

            //DrawGridCube(startingPosition);
            Gizmos.matrix = transform.localToWorldMatrix;
            foreach (Vector3 position in GetUnits())
            {
                Vector3 nodePosition = GetStartingPosition() + position;
                Color nodeColor = ValidatePosition(nodePosition);
                DrawGridCube(nodePosition, nodeColor);
            }
                

            //foreach (Vector3 position in GetUnits())
                //Gizmos.DrawWireCube(meshFilter.transform.position - (meshFilter.transform.lossyScale / 2) + position, gridScale);
        }

        private Vector3 GetStartingPosition()
        {
            Vector3 value = (transform.position - transform.lossyScale / 2) + (gridScale / 2);
            //value -= new Vector3(0.5f, 0.5f, 0.5f);
            value -= transform.lossyScale / 2;
            return (value);
        }

        public List<Vector3> GetUnits()
        {
            List<Vector3> returnList = new List<Vector3>();

            for (int y = 0; y < spawnRange.y; y++)
            {
                for (int z = 0; z < spawnRange.z; z++)
                {
                    for (int x = 0; x < spawnRange.x; x++)
                    {
                        returnList.Add(new Vector3(gridScale.x * x,gridScale.y * y,gridScale.z * z));
                        if (returnList.Count > maxCubes) return (returnList);
                    }
                }
            }

            return (returnList);
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
