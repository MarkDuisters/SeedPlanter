#if UNITY_EDITOR
using System;
using UnityEngine;

namespace MD
{
    [Serializable]
    public class OccupiedPositionInfo
    {
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
    }
}
#endif