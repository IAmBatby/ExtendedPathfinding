using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    public class PathManager : MonoBehaviour
    {
        public enum DrawMode { DefaultPaths, CompressedPaths }
        [Header("References")]
        public List<EntranceTeleport> entranceTeleports = new List<EntranceTeleport>();
        public GameObject playerShip;

        [Space(15)]
        [Header("Settings")]
        public DrawMode drawMode;
        public float mergeSampledCornersDistance;
        public int mergeSampledCornersCompressionSteps;

        private void OnDrawGizmosSelected()
        {
            GetReferences();

            if (drawMode == DrawMode.DefaultPaths)
            {

                foreach (EntranceTeleport entranceTeleport in entranceTeleports)
                {
                    if (Utilities.CalculateNewPath(playerShip.transform.position, entranceTeleport.transform.position, out NavMeshPath newPath))
                        Utilities.DrawNavmeshPath(newPath, new Vector3(1, 2.5f, 1));
                    else
                        Debug.Log("Could not visualise path");
                }
            }
            else if (drawMode == DrawMode.CompressedPaths)
            {
                List<NavMeshPath> paths = GetPointOfInterestPaths();
                List<List<Vector3>> pointCollections = CompressPointOfInterestPaths(paths, mergeSampledCornersCompressionSteps);

                foreach (List<Vector3> pointCollection in pointCollections)
                    Utilities.DrawPoints(pointCollection, new Vector3(1, 2.5f, 1));
            }
        }

        private void GetReferences()
        {
            if (entranceTeleports == null || entranceTeleports.Count == 0)
                entranceTeleports = Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None).ToList();
            if (playerShip == null)
                playerShip = GameObject.Find("PlayerShipNavmesh");
        }

        private List<NavMeshPath> GetPointOfInterestPaths()
        {
            List<NavMeshPath> returnList = new List<NavMeshPath>();
            foreach (EntranceTeleport entranceTeleport in entranceTeleports)
                if (Utilities.CalculateNewPath(playerShip.transform.position, entranceTeleport.transform.position, out NavMeshPath newPath))
                    returnList.Add(newPath);
            return (returnList);
        }

        private List<List<Vector3>> CompressPointOfInterestPaths(List<NavMeshPath> pointOfInterestPaths, int steps)
        {
            List<List<Vector3>> cornerCollections = new List<List<Vector3>>();
            List<List<Vector3>> returnList = new List<List<Vector3>>();
            foreach (NavMeshPath navMeshPath in pointOfInterestPaths)
                cornerCollections.Add(new List<Vector3>(navMeshPath.corners));

            cornerCollections = CompressPointOfInterestPaths(cornerCollections);

            //foreach (List<Vector3> collection in cornerCollections)
            //returnList.Add(CompressPoints(collection, collection, mergeSampledCornersDistance));
            returnList = new List<List<Vector3>>(cornerCollections);

            return (returnList);
        }

        private List<List<Vector3>> CompressPointOfInterestPaths(List<List<Vector3>> pointOfInterestPaths)
        {
            List<List<Vector3>> cornerCollections = new List<List<Vector3>>();
            List<Vector3> comparisonPoints = pointOfInterestPaths[0];

            foreach (List<Vector3> navMeshPath in pointOfInterestPaths)
                cornerCollections.Add(CompressPoints(navMeshPath, comparisonPoints, mergeSampledCornersDistance));

            return (cornerCollections);
        }

        private List<Vector3> CompressPoints(List<Vector3> points, List<Vector3> comparisonPoints, float compressionDistance)
        {
            List<Vector3> returnList = new List<Vector3>(points);
            for (int i = 0; i < points.Count; i++)
                for (int j = 0; j < comparisonPoints.Count; j++)
                    if (Vector3.Distance(points[i], comparisonPoints[j]) < compressionDistance)
                        returnList[i] = Vector3.Lerp(points[i], comparisonPoints[j], 0.5f);
            return (returnList);
        }




    }
}
