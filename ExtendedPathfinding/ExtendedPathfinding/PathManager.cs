using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace ExtendedPathfinding.ExtendedPathfinding
{
    [CustomEditor(typeof(PathManager))]
    public class PathManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }

    public class PathManager : MonoBehaviour
    {
        public enum PathStep { None, Simplify, Snap, Inject, FilterNonUnique }
        public Dictionary<PathStep, Action<List<ExtendedPath>>> pathSteps = new Dictionary<PathStep, Action<List<ExtendedPath>>>();
        public enum DrawMode { DefaultPaths, CompressedPaths }
        [Header("Manual References")]
        public HeatManifest heatManifest;
        public RandomColorManifest randomColorManifest;

        [Header("Automatic References")]
        [SerializeReference] public List<PointOfInterest> entranceInterests = new List<PointOfInterest>();
        [SerializeReference] public PointOfInterest shipInterest;

        [Space(15)]
        [Header("Settings")]
        public float navMeshSampleDistance = 1.5f;

        public List<PathStep> ActivePathSteps = new List<PathStep>();

        [Space(15)]
        [Header("Compress Settings")]
        public float mergeSampledCornersDistance;
        public float identicalPathNodePositionThreshold;
        public float pointOfInterestCenterNormalizationThreshold;

        public enum SnappingMode { None, Before, After }
        [Space(15)]
        [Header("Compress Settings")]
        public SnappingMode snappingMode;
        public float snapDistanceOffset;

        [Space(15)]
        [Header("Inject Settings")]
        public float nodeInjectionDistance = 30f;

        private void OnDrawGizmosSelected()
        {
            GetReferences();
            InitializeStepDict();
            List<ExtendedPath> pointOfInterestPaths = GetPointOfInterestPaths();

            foreach (PathStep pathStep in ActivePathSteps)
                if (pathSteps.TryGetValue(pathStep, out Action<List<ExtendedPath>> action))
                    if (action != null)
                        action(pointOfInterestPaths);

            foreach (ExtendedPath pointOfInterestPath in pointOfInterestPaths)
            {
                Utilities.DrawExtendedPath(pointOfInterestPath, new Vector3(1, 2.5f, 1));
            }
        }

        private void GetReferences()
        {
            if (entranceInterests == null || entranceInterests.Count == 0)
                foreach (EntranceTeleport entranceTeleport in FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None))
                    entranceInterests.Add(new PointOfInterest(entranceTeleport.gameObject, Color.white));
            if (shipInterest == null || shipInterest.InterestObject == null)
                shipInterest = new PointOfInterest(GameObject.Find("PlayerShipNavmesh"), Color.white);
        }

        private void InitializeStepDict()
        {
            pathSteps.Clear();
            pathSteps.Add(PathStep.None, null);
            pathSteps.Add(PathStep.Snap, SnapExtendedPaths);
            pathSteps.Add(PathStep.Simplify, CompressExtendedPaths);
            pathSteps.Add(PathStep.Inject, InjectExtendedPaths);
            pathSteps.Add(PathStep.FilterNonUnique, FilterForUniqueExtendedPaths);
        }

        private List<ExtendedPath> GetPointOfInterestPaths()
        {
            List<ExtendedPath> returnList = new List<ExtendedPath>();
            foreach (PointOfInterest entrancePoint in entranceInterests)
                if (Utilities.CalculateNewPath(shipInterest.InterestObject.transform.position, entrancePoint.InterestObject.transform.position, out NavMeshPath newPath, navMeshSampleDistance))
                    returnList.Add(new ExtendedPath(shipInterest, entrancePoint, newPath, Color.white));

            foreach (PointOfInterest entrancePoint in entranceInterests)
                foreach (PointOfInterest secondEntrancePoint in entranceInterests)
                    if (entrancePoint != secondEntrancePoint)
                        if (Utilities.CalculateNewPath(entrancePoint.InterestObject.transform.position, secondEntrancePoint.InterestObject.transform.position, out NavMeshPath newerPath, navMeshSampleDistance))
                            returnList.Add(new ExtendedPath(entrancePoint, secondEntrancePoint, newerPath, Color.white));

            return (returnList);
        }

        private void CompressExtendedPaths(List<ExtendedPath> extendedPaths)
        {
            List<ExtendedPathNode> allNodes = new List<ExtendedPathNode>();
            List<ExtendedPathNode> hotNodes = new List<ExtendedPathNode>();
            foreach (ExtendedPath extendedPath in extendedPaths)
                foreach (ExtendedPathNode extendedPathNode in extendedPath.PathNodes)
                    allNodes.Add(extendedPathNode);

            allNodes = allNodes.OrderBy(node => Vector3.Distance(node.Position, extendedPaths.First().SourcePoint.InterestObject.transform.position)).ToList();

            List<List<ExtendedPathNode>> hotNodeCollections = new List<List<ExtendedPathNode>>();

            for (int i = 0; i < allNodes.Count; i++)
            {
                for (int j = 0; j < allNodes.Count; j++)
                {
                    ExtendedPathNode compareNode = allNodes[i];
                    ExtendedPathNode extendedNode = allNodes[j];

                    if (compareNode != extendedNode && !hotNodes.Contains(extendedNode))
                    {
                        if (CompareNodes(compareNode, extendedNode, mergeSampledCornersDistance) == true)
                        {
                            List<ExtendedPathNode> bestCollection = null;
                            float bestDistance = Mathf.Infinity;
                            for (int k = 0; k < hotNodeCollections.Count; k++)
                            {
                                if (CompareNodes(hotNodeCollections[k].First(), extendedNode, mergeSampledCornersDistance, out float firstDistance) == true)
                                {
                                    if (bestCollection == null || firstDistance < bestDistance)
                                    {
                                        bestCollection = hotNodeCollections[k];
                                        bestDistance = firstDistance;
                                    }
                                }
                                else if (CompareNodes(hotNodeCollections[k].Last(), extendedNode, mergeSampledCornersDistance, out float secondDistance) == true)
                                {
                                    if (bestCollection == null ||  secondDistance < bestDistance)
                                    {
                                        bestCollection = hotNodeCollections[k];
                                        bestDistance = secondDistance;
                                    }
                                }
                            }
                            if (bestCollection != null)
                                bestCollection.Add(extendedNode);
                            else
                                hotNodeCollections.Add(new List<ExtendedPathNode> { extendedNode });
                        }
                    }
                }
            }

            //Merge ExtendedPathNode positions based on their proximity to eachother
            int counter = 0;
            foreach (List<ExtendedPathNode> hotNodeCollection in hotNodeCollections)
            {
                List<Vector3> nodePositions = hotNodeCollection.Select(n => n.Position).ToList();
                Vector3 averageNodePosition = Utilities.GetAveragePosition(nodePositions);
                foreach (ExtendedPathNode hotNode in hotNodeCollection)
                {
                    hotNode.Color = randomColorManifest.GetColor(counter);
                    hotNode.Position = averageNodePosition;
                }
                counter++;
            }
        }

        private void SnapExtendedPaths(List<ExtendedPath> extendedPaths)
        {
            List<ExtendedPath> zDirectionPaths = new List<ExtendedPath>();
            List<ExtendedPath> xDirectionPaths = new List<ExtendedPath>();

            foreach (ExtendedPath extendedPath in extendedPaths)
            {
                Vector3 difference = extendedPath.TargetPoint.InterestObject.transform.position - extendedPath.SourcePoint.InterestObject.transform.position;
                float zDifference = Mathf.Abs(difference.z);
                float xDifference = Mathf.Abs(difference.x);

                if (zDifference > xDifference)
                    zDirectionPaths.Add(extendedPath);
                else
                    xDirectionPaths.Add(extendedPath);
            }

            Vector3 averageZTargetPosition = Utilities.GetAveragePosition(zDirectionPaths.Select(p => p.TargetPoint.InterestObject.transform.position).ToList());
            Vector3 averageXTargetPosition = Utilities.GetAveragePosition(xDirectionPaths.Select(p => p.TargetPoint.InterestObject.transform.position).ToList());

            foreach (ExtendedPath extendedPath in zDirectionPaths)
            {
                foreach (ExtendedPathNode extendedPathNode in extendedPath.PathNodes)
                {
                    float centerDistance = Vector3.Distance(extendedPathNode.Position, averageZTargetPosition);
                    float targetDistance = Vector3.Distance(extendedPathNode.Position, extendedPath.TargetPoint.InterestObject.transform.position);

                    if (centerDistance + snapDistanceOffset < targetDistance)
                        extendedPathNode.Position = new Vector3(averageZTargetPosition.x, extendedPathNode.Position.y, extendedPathNode.Position.z);
                }
            }

            foreach (ExtendedPath extendedPath in xDirectionPaths)
            {
                foreach (ExtendedPathNode extendedPathNode in extendedPath.PathNodes)
                {
                    float centerDistance = Vector3.Distance(extendedPathNode.Position, averageXTargetPosition);
                    float targetDistance = Vector3.Distance(extendedPathNode.Position, extendedPath.TargetPoint.InterestObject.transform.position);

                    if (centerDistance + snapDistanceOffset < targetDistance)
                        extendedPathNode.Position = new Vector3(extendedPathNode.Position.x, extendedPathNode.Position.y, averageXTargetPosition.z);
                }
            }
        }

        private bool CompareNodes(ExtendedPathNode firstNode, ExtendedPathNode secondNode, float maxDistance)
        {
            return (CompareNodes(firstNode, secondNode, maxDistance, out _));
        }

        private bool CompareNodes(ExtendedPathNode firstNode, ExtendedPathNode secondNode, float maxDistance, out float distance)
        {
            distance = Vector3.Distance(firstNode.Position, secondNode.Position);
            //0.1f check because some nodes are in the same place but attached to different paths rn
            if (distance > identicalPathNodePositionThreshold && distance < maxDistance)
                return (true);
            else
                return (false);
        }

        private void InjectExtendedPaths(List<ExtendedPath> extendedPaths)
        {
            for (int i = 0; i < extendedPaths.Count; i++)
                InjectNodesIntoExtendedPathRecursively(extendedPaths[i]);
        }

        private void InjectNodesIntoExtendedPathRecursively(ExtendedPath extendedPath)
        {
            bool breakEarly = false;
            for (int i = 0; i < extendedPath.PathNodes.Count; i++)
            {
                if (i != 0 && breakEarly == false)
                {
                    float distance = Vector3.Distance(extendedPath.PathNodes[i - 1].Position, extendedPath.PathNodes[i].Position);
                    if (distance > nodeInjectionDistance)
                    {
                        Vector3 lerpPosition = Vector3.Lerp(extendedPath.PathNodes[i - 1].Position, extendedPath.PathNodes[i].Position, 0.5f); ;
                        ExtendedPathNode newNode = new ExtendedPathNode(lerpPosition, extendedPath.PathNodes[i].Color);
                        extendedPath.PathNodes.Insert(i, newNode);
                        breakEarly = true;
                        InjectNodesIntoExtendedPathRecursively(extendedPath);
                    }
                }
            }
        }

        private void FilterForUniqueExtendedPaths(List<ExtendedPath> extendedPaths)
        {
            List<ExtendedPathNode> nonUniqueNodes = new List<ExtendedPathNode>();

            foreach (ExtendedPath extendedPath in extendedPaths)
            {
                foreach (ExtendedPath comparisonPath in extendedPaths)
                {
                    if (extendedPath != comparisonPath)
                    {
                        foreach (ExtendedPathNode extendedNode in extendedPath.PathNodes)
                            foreach (ExtendedPathNode comparisonNode in comparisonPath.PathNodes)
                            {
                                if (comparisonNode != extendedNode)
                                    if (CompareNodes(extendedNode, comparisonNode, Mathf.Infinity) == false)
                                        nonUniqueNodes.Add(extendedNode);
                            }
                    }
                }
            }

            foreach (ExtendedPathNode nonUniqueNode in nonUniqueNodes)
                foreach (ExtendedPath extendedPath in extendedPaths)
                    if (extendedPath.PathNodes.Contains(nonUniqueNode))
                        extendedPath.PathNodes.Remove(nonUniqueNode);
        }
    }
}
