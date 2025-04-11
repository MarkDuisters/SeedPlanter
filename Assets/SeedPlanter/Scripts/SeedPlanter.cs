#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using UnityEditor;

[ExecuteInEditMode]
public class SeedPlanter : MonoBehaviour
{
    enum WindNoiceType { Random, Perlin }
    enum SpawnShape { Sphere, HemiSphere };
    enum RayMode { SingleRaycast, Raymarching }
    [Header("Spawner options")]
    [SerializeField] SpawnShape spawnShape = SpawnShape.Sphere;
    [SerializeField] float shapeRadius = 1f;
    [SerializeField] int objectCount = 1;
    [SerializeField] bool randomizePointInShape = false;
    [SerializeField] bool allignToSurface = false;
    [SerializeField] SeedScriptableObject[] spawnList;
    [SerializeField] List<OccupiedPositionInfo> occupiedPositionsList;
    [Header("Raytrace settings")]
    [SerializeField] LayerMask layersToHit = -1;
    [Header("Raymarch settings")]
    [Tooltip("When enabled a per interval distance step will be taken. This gives the oppurtunity for a more organic position at the cost of more computation. When dissabled a single direction raycast will be used instead.")]
    [SerializeField] RayMode raytraceMode = RayMode.SingleRaycast;
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float stepDistance = 0.1f;
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] int maxSteps = 100;
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float gravity = -9.81f; // Adjusted gravity to Earth standard
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] Vector3 wind = Vector3.zero;
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] WindNoiceType windTurbulence = WindNoiceType.Random;
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float turbulence = 0.1f; // Randomizer value
    [Tooltip("Adjust the turbulence strength of the wind.")]
    [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float turbulenceStrength = 0.1f;
    [Header("Debugging")]
    public bool showDebugGizmos = false;

    [Button]
    [ContextMenu("Plant Seeds")]
    void PlantSeeds()
    {
        DestroyAllChildren();

        GeneratePositions();
        PopulatePositionsWithObjects();
    }

    void GeneratePositions()
    {
        for (int i = 0; i < objectCount; i++)
        {
            switch (raytraceMode)
            {
                case RayMode.SingleRaycast:
                    CalculateSeedPositionsRayCasting();
                    break;
                case RayMode.Raymarching:
                    CalculateSeedPositionsRayMarching();
                    break;
            }
        }
    }

    void PopulatePositionsWithObjects()
    {
        foreach (OccupiedPositionInfo info in occupiedPositionsList)
        {
            GameObject getObject = ObjectFabricator(info);
            if (getObject == null) continue;

            getObject.transform.parent = transform;
            getObject.transform.rotation = AllignToSurface(info.normal, getObject.transform.up);
        }
    }

    void CalculateSeedPositionsRayMarching()
    {
        Vector3 currentDirection = GetRayDirection().normalized;
        Vector3 currentPosition = GetPositionInSphere();
        Vector3 velocity = currentDirection * stepDistance;
        Vector3 windVelocity = wind * 0.01f;

        Vector3 gravityAcceleration = new Vector3(0, gravity, 0); // Direct gravity application
        // Wind oscillation based on time in the editor
        Vector3 windForce = wind * 0.01f;

        Vector3 oldPosition = currentPosition;

        for (int i = 0; i < maxSteps; i++)
        {
            // Apply gravity, wind, and turbulence to velocity
            windVelocity += windForce * stepDistance;
            windVelocity += windTurbulence == WindNoiceType.Perlin ? PerlinNoiseWindForce() : RandomVector();
            velocity += gravityAcceleration * stepDistance / maxSteps + windVelocity * stepDistance / maxSteps;
            currentPosition += velocity * stepDistance;
            // Check for collisions (raycasting)
            if (Physics.Raycast(oldPosition, currentPosition - oldPosition, out RaycastHit hit, stepDistance, layersToHit))
            {
                if (showDebugGizmos) Debug.DrawLine(oldPosition, hit.point, Color.green, 2f);  // Visualize the hit
                OccupiedPositionInfo info = new OccupiedPositionInfo(hit.point, false, Mathf.Abs(Vector3.Angle(Vector3.up, hit.normal)), hit.normal);

                occupiedPositionsList.Add(info);
                break;  // Stop if a collision is hit
            }

            if (showDebugGizmos) Debug.DrawLine(oldPosition, currentPosition, new Color((i + 0.01f) / maxSteps, 0f, 0f), 2f);

            oldPosition = currentPosition;
        }
    }

    void CalculateSeedPositionsRayCasting()
    {
        Vector3 currentDirection = GetRayDirection().normalized;
        Vector3 currentPosition = GetPositionInSphere();

        for (int i = 0; i < maxSteps; i++)
        {
            // Check for collisions (raycasting)
            if (Physics.Raycast(currentPosition, currentDirection, out RaycastHit hit, shapeRadius, layersToHit))
            {
                if (showDebugGizmos) Debug.DrawLine(currentPosition, hit.point, Color.green, 2f);  // Visualize the hit
                OccupiedPositionInfo info = new OccupiedPositionInfo(hit.point, false, Mathf.Abs(Vector3.Angle(Vector3.up, hit.normal)), hit.normal);

                occupiedPositionsList.Add(info);
                break;  // Stop if a collision is hit
            }

            if (showDebugGizmos) Debug.DrawRay(currentPosition, currentDirection * shapeRadius, Color.red, 2f);
        }
    }

    Vector3 RandomVector()
    {
        float x, y, z;
        x = Random.Range(-turbulence, turbulence);
        y = Random.Range(-turbulence, turbulence);
        z = Random.Range(-turbulence, turbulence);
        return new Vector3(x, y, z).normalized * turbulenceStrength;
    }

    Vector3 PerlinNoiseWindForce()
    {
        // Use Perlin noise to create smooth, oscillating turbulence.
        float timeFactor = (float)EditorApplication.timeSinceStartup;

        // Use Perlin noise to create oscillating forces over time
        float xForce = Mathf.PerlinNoise(timeFactor * turbulence, 0); // X-axis turbulence
        float yForce = Mathf.PerlinNoise(timeFactor * turbulence, timeFactor * turbulence);
        float zForce = Mathf.PerlinNoise(0, timeFactor * turbulence); // Z-axis turbulence

        float lerpedX = Mathf.Lerp(-1f, 1f, xForce);
        float lerpedY = Mathf.Lerp(-1f, 1f, yForce);
        float lerpedZ = Mathf.Lerp(-1f, 1f, zForce);


        Vector3 finalPerlin = new Vector3(lerpedX, lerpedY, lerpedZ);
        // Return the wind force, scaled by turbulence strength
        return finalPerlin.normalized * turbulenceStrength;
    }
    Vector3 GetPositionInSphere()
    {
        Vector3 position = transform.position;
        if (randomizePointInShape)
        {
            position = Random.insideUnitSphere * shapeRadius;

            switch (spawnShape)
            {
                case SpawnShape.Sphere:
                    return transform.position + position;
                case SpawnShape.HemiSphere:
                    position = transform.position + new Vector3(position.x, -Mathf.Abs(position.y), position.z); // Force downward
                    return position;
                default:
                    return position;
            }
        }
        return position;
    }

    GameObject ObjectFabricator(OccupiedPositionInfo occupiedInfo)
    {
        if (occupiedInfo.occupied) return null;//This position already has an object.

        SeedScriptableObject getSpawnObject = spawnList[Random.Range(0, spawnList.Length)]; // Get spawn object
        if (!ValidateSpawnLocation(getSpawnObject, occupiedInfo))
        {
            //   print("Invalid spawn location.");
            return null;
        }

        GameObject go = Instantiate(getSpawnObject.GetObject());
        go.transform.position = occupiedInfo.position + getSpawnObject.GetOffset();
        go.transform.rotation = getSpawnObject.GetRotationY();
        go.transform.localScale = getSpawnObject.GetScaleXYZ();
        go.transform.parent = transform;
        occupiedInfo.occupied = true;
        return go;
    }

    Vector3 GetRayDirection()
    {
        Vector3 randomDirection = transform.InverseTransformDirection(Random.onUnitSphere);
        switch (spawnShape)
        {
            case SpawnShape.Sphere:
                return randomDirection;
            case SpawnShape.HemiSphere:
                randomDirection.y = -Mathf.Abs(randomDirection.y); // Force downward
                return randomDirection;
            default:
                return randomDirection;
        }
    }

    Quaternion AllignToSurface(Vector3 normal, Vector3 upDirection)
    {
        return Quaternion.FromToRotation(upDirection, normal);
    }

    [Button]
    [ContextMenu("Destroy All Children")]
    void DestroyAllChildren()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        occupiedPositionsList = new List<OccupiedPositionInfo>();
    }

    bool ValidateSpawnLocation(SeedScriptableObject getSpawnObject, OccupiedPositionInfo occupiedInfo)
    {
        int neighbours = 0;
        for (int i = 0; i < occupiedPositionsList.Count; i++)
        {
            float distance = Vector3.Distance(occupiedInfo.position, occupiedPositionsList[i].position);
            //if we are within distance and the position is occupied, count as neighbour.
            if (distance < getSpawnObject.GetClosestAlowedNeightbour() && occupiedPositionsList[i].occupied)
            {
                neighbours++;
                if (showDebugGizmos) Debug.DrawLine(occupiedInfo.position, occupiedPositionsList[i].position, Color.magenta, 2f);
            }
            else
            {
                if (showDebugGizmos) Debug.DrawLine(occupiedInfo.position, occupiedPositionsList[i].position, new Color(0, 0, 1, 0.1f), 2);
            }

        }

        if (neighbours > 1 + getSpawnObject.GetMaximumNeighbours() || occupiedInfo.angleNormal >= getSpawnObject.GetMaxAngle())
        {
            return false; // Too many neighbors or to steep of an angle, invalid spawn
        }
        return true; // Valid spawn location
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, shapeRadius);
    }
}
#endif