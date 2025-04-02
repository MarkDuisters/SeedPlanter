using UnityEngine;

[CreateAssetMenu(fileName = "SeedData", menuName = "ScriptableObjects/SeedObjectData", order = 1)]
[SerializeField]
class SeedScriptableObject : ScriptableObject
{
    [Header("Object or Prefab")]
    [SerializeField] GameObject prefabObject;
    [Header("Spawn options")]
    [SerializeField] Vector3 offset = new Vector3(0, 0, 0);
    [SerializeField] bool randomRotationY = false;
    [SerializeField] float rotationRange = 360;
    [SerializeField] bool randomScaleXYZ;
    [SerializeField] Vector3 scaleMinimum, scaleMaximum = new Vector3(1f, 1f, 1f);


    [Header("Rules")]
    [SerializeField] float closestAlowedNeighbour;
    [SerializeField] int maximumNeighbours = 10;


    public GameObject GetObject() => prefabObject;
    public Vector3 GetOffset() => offset;
    public bool EnableRandomRotationY() => randomRotationY;
    public bool EnableRandomScale() => randomScaleXYZ;

    public float GetClosestAlowedNeightbour() => closestAlowedNeighbour;
    public int GetMaximumNeighbours() => maximumNeighbours;
    public Quaternion GetRotationY() => Quaternion.Euler(0, Random.Range(0, rotationRange), 0);
    public Vector3 GetScaleXYZ() => new Vector3(Random.Range(scaleMinimum.x, scaleMaximum.x), Random.Range(scaleMinimum.y, scaleMaximum.y), Random.Range(scaleMinimum.z, scaleMaximum.z));


}