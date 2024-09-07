using System;
using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;

namespace NavMeshMod
{
    [ExecuteAlways]
    public class SpawnedBlocker : MonoBehaviour
    {
        public NavMeshModifier navMeshModifier;
        public BoxCollider boxCollider;
        public float BlockerSize => NavMeshModSettings.Instance.blockerSize;

        public void Update()
        {
            transform.localScale = new Vector3(BlockerSize, BlockerSize, BlockerSize);
        }
    }
}
