using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace ExtendedPathfinding
{
    public static class Utilities
    {
        public static void DrawExtendedPath(ExtendedPath extendedPath, Vector3 scale)
        {
            List<(Vector3, Color)> pointInfos = extendedPath.GetPositionsWithColor();
            if (pointInfos.Count <= 1) return;
            Gizmos.DrawLine(extendedPath.SourcePoint.InterestObject.transform.position, pointInfos.First().Item1);
            for (int i = 0; i < pointInfos.Count; i++)
            {
                Gizmos.color = pointInfos[i].Item2;
                Gizmos.DrawCube(pointInfos[i].Item1, scale);
                if (i != 0)
                {
                    if (pointInfos[i - 1].Item2 != pointInfos[i].Item2)
                        Gizmos.color = Color.white;
                    Gizmos.DrawLine(pointInfos[i - 1].Item1, pointInfos[i].Item1);
                }
            }
            Gizmos.color = Color.white;
            Gizmos.DrawLine(pointInfos.Last().Item1, extendedPath.TargetPoint.InterestObject.transform.position);
        }

        public static void DrawNavmeshPath(NavMeshPath navMeshPath, Vector3 scale)
        {
            DrawPoints(navMeshPath.corners, scale);
        }

        public static void DrawPoints(Vector3[] points, Vector3 scale)
        {
            if (points.Length <= 1) return;
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawCube(points[i], scale);
                if (i != 0)
                    Gizmos.DrawLine(points[i - 1], points[i]);
            }
        }

        public static void DrawPoints(List<Vector3> points, Vector3 scale)
        {
            DrawPoints(points.ToArray(), scale);
        }

        public static bool CalculateNewPath(Vector3 firstPosition, Vector3 secondPosition, out NavMeshPath newPath, float sampleDistance = 1.5f, bool anchorPoints = true)
        {
            newPath = new NavMeshPath();
            NavMeshQueryFilter filter = new NavMeshQueryFilter();
            int WalkableArea = 1 << NavMesh.GetAreaFromName("Walkable");
            filter.areaMask = WalkableArea;
            filter.agentTypeID = 0;

            if (anchorPoints == true)
            {
                firstPosition = AnchorPosition(firstPosition);
                secondPosition = AnchorPosition(secondPosition);
            }

            if (NavMesh.SamplePosition(firstPosition, out NavMeshHit firstHit, sampleDistance, WalkableArea) == false ||
                NavMesh.SamplePosition(secondPosition, out NavMeshHit secondHit, sampleDistance, WalkableArea) == false)
            {
                Debug.Log("Failed To Sample Positions Within Given Range");
                return (false);
            }


            if (NavMesh.CalculatePath(firstHit.position, secondHit.position, filter, newPath) && newPath.status == NavMeshPathStatus.PathComplete)
                return (true);
            return (false);
        }

        public static Vector3 AnchorPosition(Vector3 position)
        {
            if (Physics.Raycast(position, Vector3.down, out RaycastHit raycastHit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
                return (raycastHit.point);
            return (position);
        }

        public static Vector3 GetAveragePosition(List<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
                return Vector3.zero;
            Vector3 meanVector = Vector3.zero;
            foreach (Vector3 pos in positions)
                meanVector += pos;
            return meanVector / positions.Count;
        }
    }
}
