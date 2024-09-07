using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace NavMeshMod
{
    public static class BlockerController
    {
        public static NavMeshModSettings Settings => NavMeshModSettings.Instance;
        public static int WalkableArea = 1 << NavMesh.GetAreaFromName("Walkable");

        [MenuItem("NavMeshMod/Check Selected Blocker")]
        public static void CheckSelectedBlocker()
        {
            if (Selection.activeGameObject.GetComponent<NavMeshBlockChecker>() != null)
                CompareBlockCheckerPoints(Selection.activeGameObject.GetComponent<NavMeshBlockChecker>());
        }

        [MenuItem("NavMeshMod/Clear All Spawned Blockers")]
        public static void ClearAllSpawnedBlockers()
        {
            foreach (SpawnedBlocker blockChecker in UnityEngine.Object.FindObjectsOfType<SpawnedBlocker>().ToList())
                UnityEngine.Object.DestroyImmediate(blockChecker.gameObject);
        }

        [MenuItem("NavMeshMod/Run Block Checking")]
        public static void RunBlockChecking()
        {
            foreach (NavMeshBlockChecker blockChecker in UnityEngine.Object.FindObjectsOfType<NavMeshBlockChecker>().ToList())
                CompareBlockCheckerPoints(blockChecker);
        }

        public static void CompareBlockCheckerPoints(NavMeshBlockChecker blockChecker)
        {
            if (NavMesh.SamplePosition(blockChecker.transform.position, out NavMeshHit blockerHit, Settings.navMeshSamplePositionRadius, WalkableArea))
            {
                Debug.DrawLine(blockerHit.position, blockerHit.position + new Vector3(0, 200, 0), Color.yellow, Settings.debugDrawRayTime);
                foreach (Transform positionOfInterest in GetPositionsOfInterest())
                {
                    if (NavMesh.SamplePosition(positionOfInterest.position, out NavMeshHit interestHit, Settings.navMeshSamplePositionRadius, WalkableArea))
                    {
                        Debug.DrawLine(interestHit.position, interestHit.position + new Vector3(0, 200, 0), Color.green, Settings.debugDrawRayTime);
                        if (DebugCalculatePath(interestHit.position, blockerHit.position, true) == true)
                            blockChecker.meshRenderer.SetSharedMaterials(new List<Material>() { Settings.validMaterial });
                        else
                            blockChecker.meshRenderer.SetSharedMaterials(new List<Material>() { Settings.invalidMaterial });
                    }
                    else
                        Debug.LogWarning("Failed To Sample Position For Position Of Interest: " + positionOfInterest.name);
                }
            }
            else
                Debug.LogWarning("Failed To Sample Position For Checker: " + blockChecker.name);
        }

        public static List<Vector3> CompareBatchPoints(List<Vector3> positions)
        {
            List<Transform> pointsOfInterest = GetPositionsOfInterest();
            List<Vector3> invalidPathPositions = new List<Vector3>();
            foreach (Vector3 position in positions)
            {
                Color positionColor = Color.white;
                bool validPosition = false;
                if (NavMesh.SamplePosition(position, out NavMeshHit blockerHit, Settings.navMeshSamplePositionRadius, WalkableArea))
                {
                    //Debug.DrawLine(blockerHit.position, blockerHit.position + new Vector3(0, 200, 0), Color.yellow, Settings.debugDrawRayTime);
                    foreach (Transform positionOfInterest in pointsOfInterest)
                    {
                        if (NavMesh.SamplePosition(positionOfInterest.position, out NavMeshHit interestHit, Settings.navMeshSamplePositionRadius, WalkableArea))
                        {
                            //Debug.DrawLine(interestHit.position, interestHit.position + new Vector3(0, 200, 0), Color.green, Settings.debugDrawRayTime);
                            if (DebugCalculatePath(interestHit.position, blockerHit.position, false) == true)
                            {
                                positionColor = Color.green;
                                validPosition = true;
                            }
                            else
                            {
                                positionColor = Color.red;
                            }
                        }
                    }
                }
                if (validPosition == false)
                    invalidPathPositions.Add(position);
                DrawSquare(position, Settings.blockerSize, positionColor, Settings.debugDrawRayTime);
            }

            return (invalidPathPositions);
        }

        public static bool DebugCalculatePath(Vector3 firstPosition, Vector3 secondPosition, bool debugPath)
        {
            NavMeshPath path = new NavMeshPath();
            NavMeshQueryFilter filter = new NavMeshQueryFilter();
            filter.areaMask = WalkableArea;
            filter.agentTypeID = 0;
            if (NavMesh.CalculatePath(firstPosition, secondPosition, filter, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                Vector3 firstPath = firstPosition;
                foreach (Vector3 position in path.corners)
                {
                    //Debug.DrawLine(firstPath, position, Color.green, Settings.debugDrawRayTime);
                    //Debug.DrawLine(position, position + new Vector3(0, 25, 0), Color.green, Settings.debugDrawRayTime);
                    firstPath = position;
                }
                return (true);
            }
            //else
                //Debug.DrawLine(firstPosition, secondPosition, Color.red, Settings.debugDrawRayTime);

            return (false);
        }

        public static List<Transform> GetPositionsOfInterest()
        {
            List<Transform> positionsOfInterest = new List<Transform>();

            GameObject playerShipNavMesh = GameObject.Find("PlayerShipNavmesh");
            if (playerShipNavMesh != null)
                positionsOfInterest.Add(playerShipNavMesh.transform);

            GameObject itemDropshipLandingPosition = GameObject.FindGameObjectWithTag("ItemShipLandingNode");
            if (itemDropshipLandingPosition != null)
                positionsOfInterest.Add(itemDropshipLandingPosition.transform);

            foreach (EntranceTeleport entranceTeleport in UnityEngine.Object.FindObjectsOfType<EntranceTeleport>())
                if (entranceTeleport.isEntranceToBuilding && entranceTeleport.entrancePoint != null)
                    positionsOfInterest.Add(entranceTeleport.entrancePoint);

            return (positionsOfInterest);
        }

        [MenuItem("NavMeshMod/Modded Bake")]
        public static void ModdedBake()
        {
            NavMeshSurface navMeshSurface = UnityEngine.Object.FindObjectOfType<NavMeshSurface>();
            navMeshSurface.BuildNavMesh();

            PlaceBlockCheckers();

            navMeshSurface.BuildNavMesh();

            ClearAllSpawnedBlockers();
        }

        [MenuItem("NavMeshMod/Place Block Checkers")]
        public static void PlaceBlockCheckers()
        {
            List<Vector3> positions = new List<Vector3>();

            Vector3 currentXPosition = new Vector3(-Settings.scanSize, 0,-Settings.scanSize);
            Vector3 currentZPosition;

            int xAmount = Mathf.RoundToInt((Settings.scanSize - -Settings.scanSize) / Settings.blockerSize);
            int zAmount = Mathf.RoundToInt((Settings.scanSize - -Settings.scanSize) / Settings.blockerSize);

            //DrawSquare(currentXPosition, Settings.blockerSize, Color.white, Settings.debugDrawRayTime);
            if (TryHitSampleAndDrawDown(currentXPosition, out Vector3 returnX1Position))
                positions.Add(returnX1Position);
            for (int i = 0; i < xAmount; i++)
            {
                currentXPosition += new Vector3(Settings.blockerSize,0,0);
                currentZPosition = currentXPosition;
                //currentZPosition += new Vector3(Settings.blockerSize, 0, 0);
                //DrawSquare(currentXPosition, Settings.blockerSize, Color.white, Settings.debugDrawRayTime);
                if (TryHitSampleAndDrawDown(currentXPosition, out Vector3 returnX2Position))
                    positions.Add(returnX2Position);
                for (int j = 0; j < zAmount; j++)
                {
                    currentZPosition += new Vector3(0, 0, Settings.blockerSize);
                    //DrawSquare(currentZPosition,Settings.blockerSize, Color.white,Settings.debugDrawRayTime);
                    if (TryHitSampleAndDrawDown(currentZPosition, out Vector3 returnPosition))
                        positions.Add(returnPosition);
                }
            }

            SpawnSpawnedBlockers(CompareBatchPoints(positions));
        }

        public static void SpawnSpawnedBlockers(List<Vector3> positions)
        {
            NavMeshSurface navMeshSurface = UnityEngine.Object.FindObjectOfType<NavMeshSurface>();
            foreach (Vector3 position in positions)
            {
                GameObject spawnedBlockerObject = GameObject.Instantiate(Settings.blockCheckerPrefab, position, Quaternion.identity, navMeshSurface.transform);
            }
        }

        public static bool TryHitSampleAndDrawDown(Vector3 position, out Vector3 returnPosition)
        {
            returnPosition = Vector3.zero;
            if (Physics.Raycast(position + new Vector3(0,300,0), position + new Vector3(0,-20000,0), out RaycastHit hit, Mathf.Infinity, Settings.raycastMask))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, Settings.navMeshSamplePositionRadius, WalkableArea))
                {
                    //DrawSquare(navHit.position, Settings.blockerSize, Color.white, Settings.debugDrawRayTime);
                    returnPosition = navHit.position;
                    return (true);
                }
            }
            return (false);
        }

        public static void DrawSquare(Vector3 position, float size, Color color, float duration)
        {
            Vector3 topLeftPosition = position + new Vector3(-(size / 2), 0, size / 2);
            Vector3 topRightPosition = position + new Vector3(size / 2, 0, size / 2);
            Vector3 bottomLeftPosition = position + new Vector3(-(size / 2), 0, -(size / 2));
            Vector3 bottomRightPosition = position + new Vector3(size / 2, 0, -(size / 2));

            Debug.DrawLine(topLeftPosition, topRightPosition, color, duration);
            Debug.DrawLine(topRightPosition, bottomRightPosition, color, duration);
            Debug.DrawLine(bottomRightPosition, bottomLeftPosition, color, duration);
            Debug.DrawLine(bottomLeftPosition, topLeftPosition, color, duration);
        }
    }
}
