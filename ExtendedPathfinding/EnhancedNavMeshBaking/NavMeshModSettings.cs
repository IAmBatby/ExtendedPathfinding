using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NavMeshMod
{
    [CreateAssetMenu(fileName = "NavMeshModSettings", menuName = "NavMeshMod/NavMeshModSettings")]
    public class NavMeshModSettings : ScriptableObject
    {
        public static NavMeshModSettings _navMeshModSettings;
        public static NavMeshModSettings Instance
        {
            get
            {
                if (_navMeshModSettings == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:NavMeshModSettings");
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _navMeshModSettings = AssetDatabase.LoadAssetAtPath<NavMeshModSettings>(path);
                }
                return (_navMeshModSettings);
            }
        }

        public float blockerSize = 25f;
        public float navMeshSamplePositionRadius = 1;
        public float debugDrawRayTime;

        public Material validMaterial;
        public Material invalidMaterial;

        public Transform startingTransform;
        public Transform endingTransform;

        public LayerMask raycastMask;
        public int scanSize = 2500;

        public GameObject blockCheckerPrefab;
    }
}
