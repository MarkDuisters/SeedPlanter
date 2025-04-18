#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace MD
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SeedPlanter))]
    public class ObjectToTerrainInstance : MonoBehaviour
    {

        [SerializeField] Terrain terrain;
        [SerializeField] SeedScriptableObject[] spawnList;
        [SerializeField] List<OccupiedPositionInfo> occupiedPositionsList;

        bool init = false;

        [Button]
        void ConvertToTerrainInstances()
        {
            Init();

            if (!init)
            {
                Debug.LogError("Initialisation failed");
                return;
            }
            RemoveAllTerrainTrees();
            AddObjectToPrototypeList();
            PaintObjectsOnTerrain();
            EditorUtility.SetDirty(terrain.terrainData);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        }



        void Init()
        {
            init = false;
            spawnList = GetComponent<SeedPlanter>().GetSeedList();
            occupiedPositionsList = GetComponent<SeedPlanter>().GetOccupiedPositionList();
            if (terrain == null || spawnList.Length == 0)
            {
                Debug.LogError("No objects found, please generate them first.");
                return;
            }
            init = true;
        }

        void AddObjectToPrototypeList()
        {
            List<TreePrototype> currentPrototypes = new List<TreePrototype>(terrain.terrainData.treePrototypes);
            HashSet<GameObject> existingPrefabs = new HashSet<GameObject>();
            //Make a cache of the already existing prototypes in order to prevent adding duplicates.
            foreach (var proto in currentPrototypes)
            {
                if (proto != null && proto.prefab != null)
                    existingPrefabs.Add(proto.prefab);
            }

            foreach (SeedScriptableObject seed in spawnList)
            {
                //only add reference after checking for duplicate entries.
                if (!existingPrefabs.Contains(seed.GetObject()))
                {
                    GameObject prefab = seed.GetObject();
                    TreePrototype newPrototype = new TreePrototype { prefab = prefab };
                    currentPrototypes.Add(newPrototype);
                    existingPrefabs.Add(prefab);//Add the prefab to the existing list in case we have double entries in the planter;
                }
            }

            terrain.terrainData.treePrototypes = currentPrototypes.ToArray();

        }

        void PaintObjectsOnTerrain()
        {
            TerrainData terrainData = terrain.terrainData;
            List<TreeInstance> currentInstances = new List<TreeInstance>(terrainData.treeInstances);

            TreePrototype[] prototypes = terrainData.treePrototypes;
            Dictionary<GameObject, int> prefabToPrototypeIndex = new Dictionary<GameObject, int>();

            // Populate dictionary for easy lookup
            for (int i = 0; i < prototypes.Length; i++)
            {
                if (prototypes[i] != null && prototypes[i].prefab != null)
                {
                    prefabToPrototypeIndex[prototypes[i].prefab] = i;
                }
            }

            foreach (OccupiedPositionInfo info in occupiedPositionsList)
            {
                if (info == null || info.connectedPrefab == null) continue;

                // Get the actual prefab asset from the scene instance
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(info.connectedPrefab);
                if (prefab == null)
                {
                    Debug.LogWarning($"Could not resolve prefab asset from instance: {info.connectedPrefab.name}");
                    continue;
                }

                if (!prefabToPrototypeIndex.TryGetValue(prefab, out int prototypeIndex))
                {
                    Debug.LogWarning($"Prefab {prefab.name} not found in tree prototypes.");
                    continue;
                }

                Vector3 worldPosition = info.connectedPrefab.transform.position;
                Vector3 terrainPosition = worldPosition - terrain.transform.position;

                // Normalize the position relative to the terrain size
                Vector3 normalizedPos = new Vector3(
                    terrainPosition.x / terrainData.size.x,
                    0,
                    terrainPosition.z / terrainData.size.z
                );

                Vector3 prefabScale = info.connectedPrefab.transform.localScale;

                TreeInstance tree = new TreeInstance
                {
                    position = normalizedPos,
                    prototypeIndex = prototypeIndex,
                    rotation = info.connectedPrefab.transform.eulerAngles.y,
                    widthScale = prefabScale.x,
                    heightScale = prefabScale.y,
                    color = Color.white,
                    lightmapColor = Color.white
                };

                currentInstances.Add(tree);
            }

            // After adding all the tree instances, update the terrain
            terrainData.SetTreeInstances(currentInstances.ToArray(), true);
            terrain.Flush();  // Ensure terrain is refreshed after updating
        }




        [Button]
        void RemoveAllTerrainTrees()
        {
            TerrainData terrainData = terrain.terrainData;
            List<TreeInstance> emptyTreeList = new List<TreeInstance>();
            terrainData.SetTreeInstances(emptyTreeList.ToArray(), true);
            EditorUtility.SetDirty(terrainData);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
            terrain.Flush();
        }

    }
}
#endif