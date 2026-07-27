using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

// Alles-in-einem-Generator: Boden + Straßenraster + Gebaeude in den Bloecken dazwischen +
// Bounty-Hunt-Gameplay-Marker, mit EINEM Knopfdruck. Baut auf den Lehren aus den vorherigen
// Versuchen auf:
// - Straßen: eigenes Prefab wird selbst gemessen (nicht geraten), genau wie in RoadGridGenerator.
// - Gebaeude: Plot-Groesse wird aus der TATSAECHLICH GEMESSENEN groessten Gebaeude-Grundflaeche
//   berechnet (Instanz erzeugen -> Bounds messen -> wieder loeschen), nicht aus einer geschaetzten Zahl.
//   Das war der Bug im allerersten Gebaeude-Generator: er hat nur geraten statt zu messen.
// - Boden wird automatisch erzeugt (das hatte der erste Versuch schlicht vergessen).
public class FullMapGenerator : MonoBehaviour
{
    [System.Serializable]
    public class BuildingTier
    {
        public string name = "Tier";
        public GameObject[] prefabs;
        public float approxRoofHeight = 10f; // fuer Sprung-/Dash-Abstand an den Block-Raendern
    }

    [Header("Auto-Populate (Editor only)")]
    [Tooltip("Root-Ordner mit deinen Synty-Prefabs, z.B. Assets/PolygonSciFiCity/Prefabs")]
    public string prefabRoot = "Assets/PolygonSciFiCity/Prefabs";

    [Header("Gebaeude")]
    public BuildingTier[] buildingTiers;
    [Tooltip("Wie viele Gebaeude-Plots pro Block (2 = 2x2 Gebaeude pro Block).")]
    public int plotsPerBlockSide = 2;
    public float plotPadding = 2f;

    [Header("Straßen")]
    public GameObject straightRoadPrefab;
    public GameObject intersectionPrefab;
    public int columns = 3;
    public int rows = 3;

    [Header("Boden")]
    public bool generateGround = true;
    public Material groundMaterial;

    [Header("Verticality")]
    public float maxRoofGap = 7f;

    [Header("Bounty-Hunt-Marker (leer lassen = Platzhalter-Wuerfel)")]
    public GameObject lootSpawnMarkerPrefab;
    public GameObject clueSpawnMarkerPrefab;
    public GameObject bossSpawnMarkerPrefab;
    public GameObject playerSpawnMarkerPrefab;
    public int clueCount = 3;
    public int playerSpawnCount = 8;

    private Transform mapRoot;
    private float roadLength;
    private float roadWidth;
    private float plotSize;
    private float blockSize;

#if UNITY_EDITOR
    [ContextMenu("Auto-Populate From Folder")]
    public void AutoPopulate()
    {
        if (!AssetDatabase.IsValidFolder(prefabRoot))
        {
            Debug.LogError($"[FullMapGenerator] Ordner nicht gefunden: {prefabRoot}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
        var byFolder = new Dictionary<string, List<GameObject>>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string folderName = Path.GetFileName(Path.GetDirectoryName(path));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            if (!byFolder.ContainsKey(folderName)) byFolder[folderName] = new List<GameObject>();
            byFolder[folderName].Add(prefab);
        }

        var tiers = new List<BuildingTier>();
        foreach (var kvp in byFolder)
        {
            if (kvp.Key.IndexOf("Building", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tiers.Add(new BuildingTier { name = kvp.Key, prefabs = kvp.Value.ToArray(), approxRoofHeight = 12f });
            }
            else if (kvp.Key.IndexOf("Environment", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tiers.Add(new BuildingTier { name = kvp.Key, prefabs = kvp.Value.ToArray(), approxRoofHeight = 4f });
            }
            else if (kvp.Key.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 && kvp.Value.Count > 0)
            {
                straightRoadPrefab = kvp.Value[0];
                if (kvp.Value.Count > 1) intersectionPrefab = kvp.Value[1];
            }
        }

        buildingTiers = tiers.ToArray();
        Debug.Log($"[FullMapGenerator] Gefunden: {buildingTiers.Length} Gebaeude-Tiers " +
                  $"({string.Join(", ", buildingTiers.Select(t => $"{t.name}:{t.prefabs.Length}"))}), " +
                  $"Straße: {(straightRoadPrefab != null ? straightRoadPrefab.name : "keine gefunden")}. " +
                  "Falls hier nichts/falsches steht: Prefab Root pruefen oder Felder manuell im Inspector reinziehen.");
    }
#endif

    [ContextMenu("Generate Full Map")]
    public void GenerateFullMap()
    {
        if (straightRoadPrefab == null)
        {
            Debug.LogError("[FullMapGenerator] Kein 'Straight Road Prefab' gesetzt.");
            return;
        }
        if (buildingTiers == null || buildingTiers.Length == 0)
        {
            Debug.LogError("[FullMapGenerator] Keine 'Building Tiers' gesetzt.");
            return;
        }

        ClearExisting();
        MeasureRoad();
        float largestBuildingFootprint = MeasureLargestBuildingFootprint();

        plotSize = largestBuildingFootprint + plotPadding;
        blockSize = plotsPerBlockSide * plotSize;

        mapRoot = new GameObject("Generated_Map").transform;
        mapRoot.SetParent(transform, false);

        if (generateGround)
        {
            GenerateGround();
        }

        GenerateRoads();
        var blockCenters = GenerateBuildingBlocks();
        PlaceGameplayMarkers(blockCenters);

        Debug.Log($"[FullMapGenerator] Fertig: {columns}x{rows} Bloecke, Block-Groesse {blockSize:F0}m, " +
                  $"Plot-Groesse {plotSize:F0}m (aus gemessener groesster Gebaeude-Grundflaeche {largestBuildingFootprint:F0}m), " +
                  $"Straßensegment-Laenge {roadLength:F1}m. {clueCount} Hinweise, 1 Boss, {playerSpawnCount} Spieler-Spawns.");
    }

    private void MeasureRoad()
    {
        GameObject temp = Instantiate(straightRoadPrefab);
        var renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            roadLength = 10f;
            roadWidth = 6f;
        }
        else
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            roadLength = Mathf.Max(b.size.x, b.size.z);
            roadWidth = Mathf.Min(b.size.x, b.size.z);
        }
        DestroyImmediate(temp);
    }

    private float MeasureLargestBuildingFootprint()
    {
        float largest = 10f;
        var checkedPrefabs = new HashSet<GameObject>();

        foreach (var tier in buildingTiers)
        {
            if (tier.prefabs == null) continue;
            foreach (var prefab in tier.prefabs)
            {
                if (prefab == null || checkedPrefabs.Contains(prefab)) continue;
                checkedPrefabs.Add(prefab);

                GameObject temp = Instantiate(prefab);
                var renderers = temp.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    float footprint = Mathf.Max(b.size.x, b.size.z);
                    if (footprint > largest) largest = footprint;
                }
                DestroyImmediate(temp);
            }
        }
        return largest;
    }

    private void GenerateGround()
    {
        float totalWidth = columns * blockSize + (columns + 1) * roadWidth;
        float totalDepth = rows * blockSize + (rows + 1) * roadWidth;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(mapRoot, false);
        ground.transform.localScale = new Vector3(totalWidth / 10f, 1f, totalDepth / 10f);
        ground.transform.position = new Vector3(totalWidth * 0.5f, -0.05f, totalDepth * 0.5f);
        if (groundMaterial != null) ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
    }

    private void GenerateRoads()
    {
        Transform roadRoot = new GameObject("Roads").transform;
        roadRoot.SetParent(mapRoot, false);

        float cell = blockSize + roadWidth;

        for (int r = 0; r <= rows; r++)
        {
            float zPos = r * cell;
            int segCount = Mathf.CeilToInt((columns * cell) / roadLength);
            for (int i = 0; i < segCount; i++)
            {
                float xPos = i * roadLength + roadLength * 0.5f;
                GameObject seg = Instantiate(straightRoadPrefab, new Vector3(xPos, 0, zPos), Quaternion.Euler(0, 90, 0), roadRoot);
                seg.name = $"Road_H_{r}_{i}";
            }
        }

        for (int c = 0; c <= columns; c++)
        {
            float xPos = c * cell;
            int segCount = Mathf.CeilToInt((rows * cell) / roadLength);
            for (int i = 0; i < segCount; i++)
            {
                float zPos = i * roadLength + roadLength * 0.5f;
                GameObject seg = Instantiate(straightRoadPrefab, new Vector3(xPos, 0, zPos), Quaternion.identity, roadRoot);
                seg.name = $"Road_V_{c}_{i}";
            }
        }

        if (intersectionPrefab != null)
        {
            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= columns; c++)
                {
                    Vector3 pos = new Vector3(c * cell, 0, r * cell);
                    GameObject cross = Instantiate(intersectionPrefab, pos, Quaternion.identity, roadRoot);
                    cross.name = $"Intersection_{c}_{r}";
                }
            }
        }
    }

    private List<Vector3> GenerateBuildingBlocks()
    {
        Transform buildingRoot = new GameObject("Buildings").transform;
        buildingRoot.SetParent(mapRoot, false);

        float cell = blockSize + roadWidth;
        var blockCenters = new List<Vector3>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 blockOrigin = new Vector3(c * cell + roadWidth, 0, r * cell + roadWidth);
                Transform blockRoot = new GameObject($"Block_{c}_{r}").transform;
                blockRoot.SetParent(buildingRoot, false);

                for (int z = 0; z < plotsPerBlockSide; z++)
                {
                    for (int x = 0; x < plotsPerBlockSide; x++)
                    {
                        Vector3 plotCenter = blockOrigin + new Vector3(
                            x * plotSize + plotSize * 0.5f,
                            0,
                            z * plotSize + plotSize * 0.5f
                        );

                        bool isEdgePlot = x == 0 || z == 0 || x == plotsPerBlockSide - 1 || z == plotsPerBlockSide - 1;
                        BuildingTier tier = PickTier(isEdgePlot);
                        if (tier == null || tier.prefabs == null || tier.prefabs.Length == 0) continue;

                        GameObject prefab = tier.prefabs[Random.Range(0, tier.prefabs.Length)];
                        float rotationY = Random.Range(0, 4) * 90f;
                        GameObject instance = Instantiate(prefab, plotCenter, Quaternion.Euler(0, rotationY, 0), blockRoot);
                        instance.name = $"{prefab.name}_{x}_{z}";
                    }
                }

                blockCenters.Add(blockOrigin + new Vector3(blockSize * 0.5f, 0, blockSize * 0.5f));
            }
        }

        return blockCenters;
    }

    private BuildingTier PickTier(bool isEdgePlot)
    {
        if (isEdgePlot)
        {
            BuildingTier lowest = buildingTiers[0];
            foreach (var t in buildingTiers) if (t.approxRoofHeight < lowest.approxRoofHeight) lowest = t;
            return lowest;
        }
        return buildingTiers[Random.Range(0, buildingTiers.Length)];
    }

    private void PlaceGameplayMarkers(List<Vector3> blockCenters)
    {
        Transform markerRoot = new GameObject("Gameplay_Markers").transform;
        markerRoot.SetParent(mapRoot, false);

        for (int i = 0; i < clueCount && i < blockCenters.Count; i++)
        {
            int index = Mathf.FloorToInt((float)blockCenters.Count / clueCount) * i;
            SpawnMarker(clueSpawnMarkerPrefab, blockCenters[index], markerRoot, $"ClueSpawn_{i}");
        }

        Vector3 mapCenter = Vector3.zero;
        foreach (var c in blockCenters) mapCenter += c;
        mapCenter /= Mathf.Max(1, blockCenters.Count);
        SpawnMarker(bossSpawnMarkerPrefab, mapCenter, markerRoot, "BossSpawn");

        int lootIndex = 0;
        foreach (var center in blockCenters)
        {
            Vector3 pos = center + new Vector3(Random.Range(-blockSize * 0.3f, blockSize * 0.3f), 0, Random.Range(-blockSize * 0.3f, blockSize * 0.3f));
            SpawnMarker(lootSpawnMarkerPrefab, pos, markerRoot, $"LootSpawn_{lootIndex}");
            lootIndex++;
        }

        int spawnsPerBlock = Mathf.CeilToInt((float)playerSpawnCount / blockCenters.Count);
        int spawnIndex = 0;
        foreach (var center in blockCenters)
        {
            for (int i = 0; i < spawnsPerBlock && spawnIndex < playerSpawnCount; i++)
            {
                float angle = (360f / spawnsPerBlock) * i;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * (blockSize * 0.4f);
                SpawnMarker(playerSpawnMarkerPrefab, center + offset, markerRoot, $"PlayerSpawn_{spawnIndex}");
                spawnIndex++;
            }
        }
    }

    private void SpawnMarker(GameObject prefab, Vector3 position, Transform parent, string markerName)
    {
        GameObject marker;
        if (prefab != null)
        {
            marker = Instantiate(prefab, position, Quaternion.identity, parent);
        }
        else
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.localScale = Vector3.one * 1.5f;
            marker.transform.position = position;
            marker.transform.SetParent(parent, true);
        }
        marker.name = markerName;
    }

    private void ClearExisting()
    {
        Transform existing = transform.Find("Generated_Map");
        if (existing != null) DestroyImmediate(existing.gameObject);
    }
}
