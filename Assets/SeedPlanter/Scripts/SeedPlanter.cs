#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class SeedPlanter : MonoBehaviour
{
    enum SpawnShape { Sphere, HemiSphere };
    [Header("Spawner options")]
    [SerializeField] SpawnShape spawnShape = SpawnShape.Sphere;
    [SerializeField] float shapeRadius = 1f;
    [SerializeField] int objectCount = 1;
    [SerializeField] bool randomizePointInShape = false;
    [SerializeField] bool allignToSurface = false;
    [SerializeField] LayerMask layersToHit = -1;
    [SerializeField] SeedScriptableObject[] spawnList;
    [SerializeField] List<Vector3> occupiedPositionsList;
    [Header("Debugging")]
    public bool showDebugGizmos = false;

    [Button]
    [ContextMenu("Plant Seeds")]
    void PlantSeeds()
    {

        DestroyAllChildren();

        int i = 0;
        while (i < objectCount)
        {

            Vector3 rayDirection = GetRayDirection();
            Vector3 position = GetPositionInSphere();
            Ray ray = new Ray(position, rayDirection);

            if (showDebugGizmos) Debug.DrawRay(ray.origin, ray.direction * shapeRadius, Color.blue, 2f);

            if (Physics.Raycast(ray, out RaycastHit hit, shapeRadius, layersToHit))
            {
                Transform go = ObjectFabricator(hit.point)?.transform;
                if (go == null) return;
                go.rotation = AllignToSurface(hit.normal, go.up) * go.rotation;

                if (showDebugGizmos) Debug.DrawLine(position, hit.point, Color.green, 2f);
            }

            i++;

        }
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
                    position = transform.position + new Vector3(position.x, -Mathf.Abs(position.y), position.z);//force down
                    return position;
                default:
                    return position;
            }
        }
        return position;

    }

    //bug bomen ignore max neightbours validation. Waarschijnlijk omdat alles tegelijk in 1 frame spawned zien ze elkaar niet.
    //mogelijke oplossing, hou een lijst van posities bij van reeds gespawnde objecten.
    //Maak een methode die op basis van afstand kandidaten zoekt en optelt hoeveel er zijn. 
    //return true als het aantal objecten kleiner is dan toegestaan, false als het er teveel zijn.

    GameObject ObjectFabricator(Vector3 intersectionPoint)
    {
        SeedScriptableObject getSpawnObject = spawnList[Random.Range(0, spawnList.Length)];//get our object storing the prefab and other info.
                                                                                           //if false do not spawn, if true continue
        if (!ValidateSpawnLocation(intersectionPoint, getSpawnObject, occupiedPositionsList))
        {
            print("invalid.");
            return null;
        }
        GameObject go = Instantiate(getSpawnObject.GetObject());
        go.transform.position = intersectionPoint + getSpawnObject.GetOffset();
        occupiedPositionsList.Add(intersectionPoint);// spawned position to list
        go.transform.rotation = getSpawnObject.GetRotationY();

        go.transform.localScale = getSpawnObject.GetScaleXYZ();
        go.transform.parent = transform;

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
                randomDirection.y = -Mathf.Abs(randomDirection.y);//force down
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
        occupiedPositionsList = new List<Vector3>();
    }


    //Only return true if there are no more than 1 colliders in the overlap sphere. The 1 threshold accounts for the terrain collider and the object itself.
    bool ValidateSpawnLocation(Vector3 intersectPoint, SeedScriptableObject getSpawnObject, List<Vector3> occupied)
    {
        int neightbours = 0;
        int i = 0;
        while (i < occupied.Count)
        {
            float distance = Vector3.Distance(intersectPoint, occupied[i]);
            if (distance < getSpawnObject.GetClosestAlowedNeightbour())
            {
                neightbours++;
                if (showDebugGizmos) Debug.DrawLine(intersectPoint, occupied[i], Color.red, 3f);
            }
            i++;
        }

        if (neightbours > 1 + getSpawnObject.GetMaximumNeighbours())//always add 1 to take the initial hit collider or terrain into account.
        {
            //when the length is larger than the maximum allowed neightbbour we have an invalid spawn location.
            return false;
        }
        else
        {
            //when the length is 1+maximum Neightbours or less we have a valid location.
            return true;
        }
    }


    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        Gizmos.color = new Color(0, 1, 0, 0.5f);

        Gizmos.DrawWireSphere(transform.position, shapeRadius);


    }



}

#endif