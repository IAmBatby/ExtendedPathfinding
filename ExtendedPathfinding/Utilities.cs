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
    }
}
