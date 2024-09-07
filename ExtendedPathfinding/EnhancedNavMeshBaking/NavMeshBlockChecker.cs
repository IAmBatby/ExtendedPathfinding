using System;
using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace NavMeshMod
{
    [ExecuteAlways]
    public class NavMeshBlockChecker : MonoBehaviour
    {
        public NavMeshModifier navMeshModifier;
        public SphereCollider collider;
        public MeshRenderer meshRenderer;
        public float BlockerSize => NavMeshModSettings.Instance.blockerSize;

        public void Awake()
        {
            navMeshModifier = GetComponent<NavMeshModifier>();
            collider = GetComponent<SphereCollider>();
            meshRenderer = GetComponent<MeshRenderer>();
            collider.enabled = false;
            if (navMeshModifier != null)
                navMeshModifier.enabled = false;
            else
                enabled = false;
        }

        bool hasRan = false;
        public void Update()
        {
            transform.localScale = new Vector3(BlockerSize, BlockerSize, BlockerSize);

            if (hasRan == false && Selection.activeObject == this)
            {
                BlockerController.CompareBlockCheckerPoints(this);
                hasRan = true;
            }
            else if (Selection.activeObject != this)
                hasRan = false;
        }
    }
}
