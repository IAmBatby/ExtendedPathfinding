using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGrid : MonoBehaviour
{
    public MeshCollider terrainCollider;

    public Vector3 cubeSize;

    public void OnDrawGizmos()
    {
        if (terrainCollider == null) return;


        int xAmount = Mathf.RoundToInt((terrainCollider.bounds.extents * 2).x / cubeSize.x);
        int zAmount = Mathf.RoundToInt((terrainCollider.bounds.extents * 2).z / cubeSize.z);
        int yAmount = Mathf.RoundToInt((terrainCollider.bounds.extents * 2).y / cubeSize.y);

        Vector3 positionOffset = Vector3.zero;

        for (int y = 0; y < yAmount; y++)
        {
            for (int x = 0; x < xAmount; x++)
            {
                for (int z = 0; z < zAmount; z++)
                {
                    Gizmos.DrawWireCube((terrainCollider.bounds.center - terrainCollider.bounds.extents) + (cubeSize / 2) + positionOffset, cubeSize);
                    positionOffset += new Vector3(0, 0, cubeSize.z);
                }
                positionOffset = new Vector3(positionOffset.x, 0, 0);
                Gizmos.DrawWireCube((terrainCollider.bounds.center - terrainCollider.bounds.extents) + (cubeSize / 2) + positionOffset, cubeSize);
                positionOffset += new Vector3(cubeSize.x, 0, 0);
            }
            positionOffset = new Vector3(0, positionOffset.y, 0);
            Gizmos.DrawWireCube((terrainCollider.bounds.center - terrainCollider.bounds.extents) + (cubeSize / 2) + positionOffset, cubeSize);
            positionOffset += new Vector3(0, cubeSize.y, 0);
        }
    }
}
