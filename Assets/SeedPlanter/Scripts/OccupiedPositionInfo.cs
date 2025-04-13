#if UNITY_EDITOR
using System;
using UnityEngine;

[Serializable]
public class OccupiedPositionInfo
{
   // [ReadOnly] public GameObject objectReference;
    [ReadOnly] public Vector3 position;
    [ReadOnly] public bool occupied;
    [ReadOnly] public float angleNormal;
    [ReadOnly] public Vector3 normal;

    public OccupiedPositionInfo(Vector3 position, bool occupied, float angleNormal, Vector3 normal)
    {
        this.position = position;
        this.occupied = occupied;
        this.angleNormal = angleNormal;
        this.normal = normal;
    }

    // public void SetObjectReference(GameObject objectReference) => this.objectReference = objectReference;
    // public GameObject GetObjectReference() => objectReference;

}

#endif