#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using UnityEditor;
using JimmysUnityUtilities;
using System.Collections;
using UnityEditor.PackageManager;
using System;


namespace MD
{
    //  [ExecuteInEditMode]
    public class SeedPlanter : MonoBehaviour
    {
        enum SpawnShape { Sphere, HemiSphere, Circle, HemiCircle };
        [Header("Spawner options")]
        [SerializeField] SpawnShape spawnShape = SpawnShape.Sphere;
        [SerializeField] float shapeRadius = 1f;
        [SerializeField] int maxPositions = 1;
        [SerializeField] bool randomizePointInShape = false;
        [SerializeField] bool allignToSurface = false;
        enum MatchType { none, material, texture }
        [Tooltip("When enabled, instead of randomly spawning an object, collect a list and spawn an object based on a seed's material or texture list. none: no cost, material: a bit expensive, texture: very expensive, especially on terrains.")]
        [SerializeField] MatchType matchType = MatchType.material;
        [Tooltip("Amount of times the system should try to fill unoccupied spaces.")]
        [SerializeField] int populationPasses = 1;
        [SerializeField] SeedScriptableObject[] spawnList;
        [SerializeField] List<OccupiedPositionInfo> occupiedPositionsList;
        [SerializeField][ReadOnly] int remainingPositions;
        [Header("Raytrace settings")]
        [SerializeField] LayerMask layersToHit = -1;
        enum RayMode { SingleRaycast, Raymarching }
        [Header("Raymarch settings")]
        [Tooltip("Raycast: Linear, fast and cheap. Raymarching:Flexible, expensive, slower with smaller steps. When enabled a per interval distance step will be taken. This gives the oppurtunity for a more organic position at the cost of more computation.")]
        [SerializeField] RayMode raytraceMode = RayMode.SingleRaycast;
        [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float stepDistance = 0.1f;
        [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float gravity = -9.81f; // Adjusted gravity to Earth standard
        [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] Vector3 wind = Vector3.zero;
        enum WindNoiseType { Random, Perlin }
        [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] WindNoiseType windTurbulence = WindNoiseType.Random;
        [Tooltip("Adjust the turbulence of the wind.")]
        [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float turbulence = 1.2f; // Randomizer value
        [Tooltip("Adjust the turbulence strength.")]
        [ShowIfEnum("raytraceMode", RayMode.Raymarching)][SerializeField] float turbulenceStrength = 0.44f;

        enum DebugInfo { None, All, Seeds, Neighbours }
        [Header("Debugging")]
        [SerializeField] DebugInfo debugInfo = DebugInfo.All;
        public bool showRadiusGizmos = false;
        #region //Planter logic
        [Button]
        void AutoPlantSeeds()
        {

            DestroyAllChildren();
            //   plantCoroutine = EditorCoroutineUtility.StartCoroutine(GeneratePositions(), this);
            //  populateCoroutine = EditorCoroutineUtility.StartCoroutine(PopulatePositionsWithObjects(), this);
            GeneratePositions();
            PopulatePositionsWithObjects();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(transform.gameObject.scene);
        }


        void GeneratePositions()
        {
            int i = 0;
            while (i < maxPositions)
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
                if (i % 1 == 0)
                {
                    float progress = (float)i / (float)maxPositions;
                    EditorUtility.DisplayProgressBar("Generating Positions", $"Step {i} of {maxPositions}", progress);
                }

                i++;
            }
            // When done, clear the progress bar
            EditorUtility.ClearProgressBar();
        }

        void PopulatePositionsWithObjects()
        {
            for (int i = populationPasses; i > 0; i--)
            {
                int counter = 0;
                foreach (OccupiedPositionInfo info in occupiedPositionsList)
                {
                    GameObject getObject = ObjectFabricator(info);
                    if (getObject == null) continue;
                    getObject.transform.parent = transform;
                    remainingPositions--;

                    if (counter % 1 == 0)
                    {
                        // Update the progress bar
                        float progress = (float)counter / (float)maxPositions;
                        EditorUtility.DisplayProgressBar("Placing objects", $"pass {i} of {populationPasses}, {getObject.name}", progress);
                    }
                    counter++;
                }
            }
            EditorUtility.ClearProgressBar();
        }

        void CalculateSeedPositionsRayCasting()
        {
            Vector3 currentDirection = GetRayDirection().normalized;
            Vector3 currentPosition = GetPositionInSphere();

            //  for (int i = 0; i < maxPositions; i++)
            //  {
            // Check for collisions (raycasting)
            SpecialRaycast(currentPosition, currentDirection, shapeRadius, layersToHit);

            if (debugInfo == DebugInfo.Seeds || debugInfo == DebugInfo.All) Debug.DrawRay(currentPosition, currentDirection * shapeRadius, Color.red, 2f);
            //  }
            remainingPositions = occupiedPositionsList.Count;
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
            bool hitSomething = false;
            int currentStep = 0;

            Perlin noise = new Perlin();

            int _maxSteps = (int)(shapeRadius / stepDistance);
            while (!hitSomething && currentStep < _maxSteps)
            {
                int noiseSeed = Random.Range(0, maxPositions);
                // Apply gravity, wind, and turbulence to velocity
                windVelocity += windForce * stepDistance;
                Vector3 noisePattern = windTurbulence == WindNoiseType.Perlin ? PerlinNoiseWindForce(noise, noiseSeed, currentStep) : RandomVector();
                windVelocity += noisePattern;
                velocity += (gravityAcceleration + noisePattern) * stepDistance / _maxSteps + windVelocity * stepDistance / _maxSteps;
                currentPosition += velocity * stepDistance;
                Vector3 segment = currentPosition - oldPosition;
                float segmentLength = segment.magnitude;

                if (SpecialRaycast(oldPosition, segment.normalized, segmentLength, layersToHit))
                {
                    hitSomething = true;
                }

                if (currentStep % 2 == 0)
                {
                    if (debugInfo == DebugInfo.Seeds || debugInfo == DebugInfo.All) Debug.DrawLine(oldPosition, currentPosition, Color.red, 2f);
                }
                oldPosition = currentPosition;
                currentStep++;
            }
            remainingPositions = occupiedPositionsList.Count;
        }

        bool SpecialRaycast(Vector3 pos, Vector3 dir, float dist, LayerMask layersToHit)
        {
            RaycastHit hit;
            if (Physics.Raycast(pos, dir, out hit, dist, layersToHit))
            {

                Collider col = hit.collider;
                Terrain terrain = col.GetComponent<Terrain>();
                Material getMaterial = null;
                if (terrain == null && matchType == MatchType.material) getMaterial = col.GetComponent<Renderer>().sharedMaterial;
                Texture2D getTexture = null;
                if (matchType == MatchType.texture)
                {
                    getTexture = TryGetTexture(getMaterial);

                    if (terrain != null)
                    {

                        TerrainTextureDetector detector = col.GetComponent<TerrainTextureDetector>();
                        if (detector == null) col.gameObject.AddComponent<TerrainTextureDetector>();
                        int index = detector.GetDominantTextureIndexAt(hit.point);
                        if (index >= 0 && index < terrain.terrainData.terrainLayers.Length)
                        {
                            getTexture = terrain.terrainData.terrainLayers[index].diffuseTexture;
                        }
                    }
                }
                float angle = Mathf.Abs(Vector3.Angle(Vector3.up, hit.normal));
                OccupiedPositionInfo info = new OccupiedPositionInfo(hit.point, false, angle, hit.normal, getMaterial, getTexture);

                occupiedPositionsList.Add(info);
                return true;
            }
            return false;
        }

        Texture2D TryGetTexture(Material mat)
        {
            if (mat == null) return null;

            string[] propertyNames = { "_Main", "_BaseMap", "_BaseColorMap", "_MainTex", "_Diffuse", "_Albedo" };

            foreach (var name in propertyNames)
            {
                if (mat.HasProperty(name))
                {
                    return mat.GetTexture(name) as Texture2D;
                }
            }

            return null;
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
                case SpawnShape.Circle:
                    return randomDirection;
                case SpawnShape.HemiCircle:
                    randomDirection.y = -Mathf.Abs(randomDirection.y); // Force downward
                    return randomDirection;
                default:
                    return randomDirection;
            }
        }

        Vector3 RandomVector()
        {
            float x, y, z;
            x = Random.Range(-turbulence, turbulence);
            y = Random.Range(-turbulence, turbulence);
            z = Random.Range(-turbulence, turbulence);
            return new Vector3(x, y, z) * turbulenceStrength;
        }


        Vector3 PerlinNoiseWindForce(Perlin noise, int seed, float tick)
        {
            float t = seed + tick * (1f / (turbulence + 0.001f)); // prevents div by zero and keeps smooth
                                                                  // Use scaled and offset coordinates to ensure variation between axes
            double x = noise.perlin(t + 100.123, t + 200.456, t + 300.789);
            double y = noise.perlin(t + 400.123, t + 500.456, t + 600.789);
            double z = noise.perlin(t + 700.123, t + 800.456, t + 900.789);

            float lerpedX = Mathf.Lerp(-1f, 1f, (float)x);
            float lerpedY = Mathf.Lerp(-1f, 1f, (float)y);
            float lerpedZ = Mathf.Lerp(-1f, 1f, (float)z);
            Vector3 wind = new Vector3(lerpedX, lerpedY, lerpedZ) * turbulenceStrength;
            return wind;
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
                    case SpawnShape.Circle:
                        position = transform.position + new Vector3(position.x, 0, position.z); // Force downward
                        return position;
                    case SpawnShape.HemiCircle:
                        position = transform.position + new Vector3(position.x, 0, position.z); // Force downward
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
            SeedScriptableObject getSpawnObject = null;
            //Place a random object or search based on a matching material in the seed.
            switch (matchType)
            {
                case MatchType.none:
                    getSpawnObject = spawnList[Random.Range(0, spawnList.Length)];
                    break;
                case MatchType.material:
                    getSpawnObject = FindObjectWithMatchingMaterials(occupiedInfo.material);
                    break;
                case MatchType.texture:
                    getSpawnObject = FindObjectWithMatchingTextures(occupiedInfo.texture);
                    break;
            }

            if (getSpawnObject == null) return null;
            //Is our location valid to spawn or do we already have a similar object?
            if (!ValidateNeighbours(getSpawnObject, occupiedInfo))
            {
                //   print("Invalid spawn location.");
                return null;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(getSpawnObject.GetObject());
            // occupiedInfo.SetObjectReference(go);//register the object in the current occupied info object.
            go.transform.position = occupiedInfo.position + getSpawnObject.GetOffset();

            if (allignToSurface) go.transform.rotation = AllignToSurface(occupiedInfo.normal, go.transform);
            if (getSpawnObject.EnableRandomRotationY()) go.transform.rotation *= Quaternion.Euler(new Vector3(0, getSpawnObject.GetRotationY(), 0));

            go.transform.localScale = getSpawnObject.GetScaleXYZ();
            go.transform.parent = transform;
            occupiedInfo.occupied = true;
            return go;
        }



        Quaternion AllignToSurface(Vector3 normal, Transform tr)
        {

            Quaternion alignmentRotation = Quaternion.FromToRotation(tr.up, normal);

            return alignmentRotation;
        }

        [Button]
        void DestroyAllChildren()
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            occupiedPositionsList = new List<OccupiedPositionInfo>();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(transform.gameObject.scene);

        }

        bool ValidateNeighbours(SeedScriptableObject getSpawnObject, OccupiedPositionInfo occupiedInfo)
        {
            int neighbours = 0;
            for (int i = 0; i < occupiedPositionsList.Count; i++)
            {
                float distance = Vector3.Distance(occupiedInfo.position, occupiedPositionsList[i].position);
                //if we are within distance and the position is occupied, count as neighbour.
                if (distance < getSpawnObject.GetClosestAlowedNeightbour() && occupiedPositionsList[i].occupied)
                {
                    neighbours++;
                    if (debugInfo == DebugInfo.Neighbours || debugInfo == DebugInfo.All) Debug.DrawLine(occupiedInfo.position, occupiedPositionsList[i].position, Color.blue, 2f);
                }

            }

            if (neighbours > getSpawnObject.GetMaximumNeighbours() || occupiedInfo.angleNormal >= getSpawnObject.GetMaxAngle())
            {
                return false; // Too many neighbors or to steep of an angle, invalid spawn
            }
            return true; // Valid spawn location
        }


        SeedScriptableObject FindObjectWithMatchingMaterials(Material mat)
        {
            if (mat == null) return null;
            List<SeedScriptableObject> viableSeeds = new List<SeedScriptableObject>();
            foreach (SeedScriptableObject seed in spawnList)
            {
                if (FindMatchingMaterial(mat, seed.GetViableMaterials()))
                {
                    viableSeeds.Add(seed);
                }
            }
            if (viableSeeds.Count > 0)
                return viableSeeds[Random.Range(0, viableSeeds.Count)];//From the available seeds randomly pick one.
            else
                return null;

        }
        bool FindMatchingMaterial(Material mat, Material[] seedMaterials)
        {

            for (int i = 0; i < seedMaterials.Length; i++)
            {
                if (seedMaterials[i] == mat) return true;//if we find any viable match, return true
            }
            //if not always return false;
            return false;
        }


        SeedScriptableObject FindObjectWithMatchingTextures(Texture2D tex)
        {
            if (tex == null) return null;
            List<SeedScriptableObject> viableSeeds = new List<SeedScriptableObject>();
            foreach (SeedScriptableObject seed in spawnList)
            {
                if (FindMatchingTexture(tex, seed.GetViableTextures()))
                {
                    viableSeeds.Add(seed);
                }
            }
            if (viableSeeds.Count > 0)
                return viableSeeds[Random.Range(0, viableSeeds.Count)];//From the available seeds randomly pick one.
            else
                return null;

        }
        bool FindMatchingTexture(Texture2D tex, Texture2D[] seedTextures)
        {

            for (int i = 0; i < seedTextures.Length; i++)
            {
                if (seedTextures[i] == tex) return true;//if we find any viable match, return true
            }
            //if not always return false;
            return false;
        }

        void OnDisable()
        {
            ClearProgressBar();
        }

        [Button]
        void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        void OnDrawGizmos()
        {
            if (!showRadiusGizmos) return;
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, shapeRadius);
        }
        #endregion

        #region //Callbacks for ObjectToTerrainInsance 

        public SeedScriptableObject[] GetSeedList() => spawnList;
        public List<OccupiedPositionInfo> GetOccupiedPositionList() => occupiedPositionsList;
        #endregion
    }
}

#endif