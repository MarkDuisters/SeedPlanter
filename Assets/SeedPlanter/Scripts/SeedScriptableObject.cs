using UnityEngine;
namespace MD
{
    [CreateAssetMenu(fileName = "SeedData", menuName = "ScriptableObjects/SeedObjectData", order = 1)]
    public class SeedScriptableObject : ScriptableObject
    {
        [Header("Object or Prefab")]
        [SerializeField] GameObject prefabObject;
        [SerializeField] Material[] viableMaterials;

        [Header("Transform options")]
        [Tooltip("Set the look of the object when spawned by adjusting its transform components.")]
        [SerializeField] Vector3 offset = new Vector3(0, 0, 0);
        [SerializeField] bool randomRotationY = false;
        [SerializeField] float rotationRange = 360;
        public enum RandomMode { None, uniform, RandomXYZ }
        [SerializeField] RandomMode randomMode;
        [ShowIfEnum("randomMode", RandomMode.RandomXYZ)][SerializeField] Vector3 scaleMinimum = new Vector3(1f, 1f, 1f), scaleMaximum = new Vector3(1f, 1f, 1f);
        [ShowIfEnum("randomMode", RandomMode.uniform)][SerializeField] float UniformScaleMinimum = 1f, UniformScaleMaximum = 1f;


        [Header("Rules")]
        [Tooltip("Closest allowed neighbour in units/meter for this system. Note that ANY occupied position in a single system counts as a neightbour, no matter the size.")]
        [SerializeField] float closestAlowedNeighbour;
        [Tooltip("How many neighbours can there maximum be in order to spawn.")]
        [SerializeField] int maximumNeighbours = 10;
        [Tooltip("What is the maximum surface angle that we are allowed to spawn at.")]
        [SerializeField] float maxAngle = 90f;


        public GameObject GetObject() => prefabObject;
        public Vector3 GetOffset() => offset;
        public bool EnableRandomRotationY() => randomRotationY;
        public RandomMode EnableRandomScale() => randomMode;

        public float GetClosestAlowedNeightbour() => closestAlowedNeighbour;
        public int GetMaximumNeighbours() => maximumNeighbours;
        public float GetRotationY() => Random.Range(0, rotationRange);
        public float GetMaxAngle() => maxAngle;
        public Material[] GetViableMaterials() => viableMaterials;

        public Vector3 GetScaleXYZ()
        {
            Vector3 newSize = prefabObject.transform.localScale;
            switch (randomMode)
            {
                case RandomMode.RandomXYZ:
                    newSize.x = Random.Range(scaleMinimum.x, scaleMaximum.x);
                    newSize.y = Random.Range(scaleMinimum.y, scaleMaximum.y);
                    newSize.z = Random.Range(scaleMinimum.z, scaleMaximum.z);
                    return newSize;
                case RandomMode.uniform:
                    float rng = Random.Range(UniformScaleMinimum, UniformScaleMaximum);
                    newSize = new Vector3(rng, rng, rng);
                    return newSize;

                case RandomMode.None:
                    return newSize;
            }

            return newSize;
        }
    }
}