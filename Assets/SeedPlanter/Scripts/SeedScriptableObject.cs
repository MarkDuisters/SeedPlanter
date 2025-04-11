using UnityEngine;

[CreateAssetMenu(fileName = "SeedData", menuName = "ScriptableObjects/SeedObjectData", order = 1)]
[SerializeField]
class SeedScriptableObject : ScriptableObject
{
    [Header("Object or Prefab")]
    [SerializeField] GameObject prefabObject;


    [Header("Transform options")]
    [Tooltip("Set the look of the object when spawned by adjusting its transform components.")]
    [SerializeField] Vector3 offset = new Vector3(0, 0, 0);
    [SerializeField] bool randomRotationY = false;
    [SerializeField] float rotationRange = 360;
    [SerializeField] bool randomScaleXYZ;
    [SerializeField] Vector3 scaleMinimum, scaleMaximum = new Vector3(1f, 1f, 1f);


    [Header("Rules")]
    [Tooltip("Closest allowed neighbour in units/meter for this system. Note that ANY occupied position in a single system counts as a neightbour, no matter the size.")]
    [SerializeField] float closestAlowedNeighbour;
    [Tooltip("How many neighbours can there maximum be in order to spawn.")]
    [SerializeField] int maximumNeighbours = 10;
    [Tooltip("What is the maximum surface angle that we are alowed tos pawn at.")]
    [SerializeField] float maxAngle = 90f;


    public GameObject GetObject() => prefabObject;
    public Vector3 GetOffset() => offset;
    public bool EnableRandomRotationY() => randomRotationY;
    public bool EnableRandomScale() => randomScaleXYZ;

    public float GetClosestAlowedNeightbour() => closestAlowedNeighbour;
    public int GetMaximumNeighbours() => maximumNeighbours;
    public Quaternion GetRotationY() => Quaternion.Euler(0, Random.Range(0, rotationRange), 0);
    public Vector3 GetScaleXYZ() => new Vector3(Random.Range(scaleMinimum.x, scaleMaximum.x), Random.Range(scaleMinimum.y, scaleMaximum.y), Random.Range(scaleMinimum.z, scaleMaximum.z));
    public float GetMaxAngle() => maxAngle;


}