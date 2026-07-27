using System.Collections.Generic;
using UnityEngine;

// Statt Gebaeude von Grund auf zufaellig zu platzieren (das hat zuletzt einen
// ueberlappenden Klumpen erzeugt), nimmt dieses Script die bereits gut aussehende
// Demo-Szene/den Demo-Stadtteil aus dem Synty-Pack und dupliziert sie 2x2 = 4 mal zu
// einer kompletten Karte. Deutlich risikoaermer, weil Skalierung/Abstaende/Boden/
// Strassen im Original schon stimmen -- wir wuerfeln nichts neu, wir kopieren nur.
//
// VORBEREITUNG in der Szene, bevor du das hier laufen laesst:
// 1. Alle Objekte des Demo-Stadtteils in der Hierarchy auswaehlen
// 2. Rechtsklick -> "Group Selection" (oder Strg/Cmd+G) -- das packt alles unter ein
//    neues leeres GameObject
// 3. Dieses neue GameObject hier unten bei "Source District" reinziehen
//
// Wichtig: das Original ("Source District") bleibt danach unangetastet an seinem Platz
// stehen -- es wird fuer alle 4 Kacheln jeweils eine KOPIE erzeugt, auch fuer die
// Position, an der das Original steht. Nach dem Generieren also das Original-Objekt
// deaktivieren/verstecken, sonst siehst du an einer Stelle eine doppelte Kopie.
public class TileDemoDistrict : MonoBehaviour
{
    [Header("Quelle")]
    [Tooltip("Das GameObject, unter dem der komplette, bereits gut aussehende Demo-Stadtteil liegt (siehe Anleitung oben im Script).")]
    public Transform sourceDistrict;

    [Header("Layout")]
    public int districtsPerSide = 2; // 2x2 = 4 Stadtteile
    [Tooltip("Zusaetzlicher Abstand zwischen den Stadtteil-Kopien, z.B. fuer Verbindungsstrassen.")]
    public float gapBetweenDistricts = 10f;

    [Header("Bounty-Hunt-Marker (leer lassen = Platzhalter-Wuerfel)")]
    public GameObject lootSpawnMarkerPrefab;
    public GameObject clueSpawnMarkerPrefab;
    public GameObject bossSpawnMarkerPrefab;
    public GameObject playerSpawnMarkerPrefab;
    public int clueCount = 3;
    public int playerSpawnCount = 8;

    [Header("Variation zwischen den 4 Stadtteilen (optional)")]
    [Tooltip("Gebaeude-Prefabs aus deinem Synty-Ordner (z.B. Assets/PolygonSciFiCity/Prefabs/Buildings), die zufaellig anstelle der Original-Gebaeude eingesetzt werden -- damit die 4 Stadtteile nicht wie exakte Klone aussehen.")]
    public GameObject[] alternativeBuildingPrefabs;

    [Tooltip("Namens-Filter: nur Kind-Objekte, deren Name diesen Text enthaelt, werden fuer den Austausch beruecksichtigt. Bei Synty z.B. 'Bld' oder 'Building' -- schau in der Hierarchy nach, wie die Gebaeude-Objekte in der Demo-Szene tatsaechlich heissen.")]
    public string buildingNameFilter = "Bld";

    [Range(0f, 1f)]
    [Tooltip("Wahrscheinlichkeit, dass ein einzelnes Gebaeude in einer Kopie gegen ein zufaelliges aus 'Alternative Building Prefabs' getauscht wird.")]
    public float buildingSwapChance = 0.4f;

    [Tooltip("Der allererste Stadtteil (Kopie 0,0) bleibt IMMER unveraendert -- das ist deine geprueft gut aussehende Referenz, falls die anderen 3 durch Variation seltsam aussehen sollten.")]
    public bool keepFirstDistrictUnchanged = true;

    private Transform mapRoot;

    [ContextMenu("Tile Demo District Into 4")]
    public void TileMap()
    {
        if (sourceDistrict == null)
        {
            Debug.LogError("[TileDemoDistrict] Kein 'Source District' zugewiesen -- siehe Anleitung im Script-Kommentar.");
            return;
        }

        ClearExisting();

        Bounds bounds = MeasureBounds(sourceDistrict);
        if (bounds.size.x < 0.01f || bounds.size.z < 0.01f)
        {
            Debug.LogError("[TileDemoDistrict] Gemessene Groesse ist quasi 0 -- steckt wirklich sichtbare Geometrie (mit Renderern) unter dem Source District?");
            return;
        }

        float stepX = bounds.size.x + gapBetweenDistricts;
        float stepZ = bounds.size.z + gapBetweenDistricts;

        mapRoot = new GameObject("Generated_Map").transform;
        mapRoot.SetParent(transform, false);

        var districtCenters = new List<Vector3>();

        for (int z = 0; z < districtsPerSide; z++)
        {
            for (int x = 0; x < districtsPerSide; x++)
            {
                GameObject copy = Instantiate(sourceDistrict.gameObject, mapRoot);
                Vector3 delta = new Vector3(x * stepX, 0, z * stepZ);
                copy.transform.position = sourceDistrict.position + delta;
                copy.name = $"District_{x}_{z}";
                copy.SetActive(true);

                bool isFirst = x == 0 && z == 0;
                if (!(isFirst && keepFirstDistrictUnchanged) && alternativeBuildingPrefabs != null && alternativeBuildingPrefabs.Length > 0)
                {
                    VaryBuildingsInCopy(copy);
                }

                Vector3 center = bounds.center + delta;
                districtCenters.Add(center);
            }
        }

        PlaceGameplayMarkers(districtCenters, bounds.size.x, bounds.size.z);

        Debug.Log($"[TileDemoDistrict] {districtsPerSide * districtsPerSide} Stadtteile erzeugt, je ca. " +
                  $"{bounds.size.x:F0}x{bounds.size.z:F0}m (vom Original gemessen). " +
                  "Denk dran, das originale 'Source District'-Objekt jetzt zu deaktivieren, sonst gibt's an dessen Position eine doppelte Kopie.");
    }

    // Ersetzt einen Teil der Gebaeude in einer Stadtteil-Kopie durch zufaellige
    // Alternativen, damit nicht alle 4 Stadtteile wie exakte Klone aussehen.
    // Behaelt Position/Rotation/Scale des Original-Gebaeudes bei, damit die
    // Strassen/Boden-Anordnung der Demo-Map nicht durcheinandergerät.
    private void VaryBuildingsInCopy(GameObject copy)
    {
        var allChildren = copy.GetComponentsInChildren<Transform>(true);
        int swapped = 0;

        foreach (var child in allChildren)
        {
            // child kann bereits zerstoert sein, falls sein Parent in einem frueheren
            // Schleifendurchlauf schon ausgetauscht (und damit geloescht) wurde.
            if (child == null) continue;
            if (child == copy.transform) continue;
            if (child.name.IndexOf(buildingNameFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (Random.value > buildingSwapChance) continue;

            GameObject replacement = alternativeBuildingPrefabs[Random.Range(0, alternativeBuildingPrefabs.Length)];
            if (replacement == null) continue;

            Transform parent = child.parent;
            Vector3 pos = child.position;
            Quaternion rot = child.rotation;
            Vector3 scale = child.localScale;

            GameObject newBuilding = Instantiate(replacement, pos, rot, parent);
            newBuilding.transform.localScale = scale;
            newBuilding.name = child.name + "_swapped";

            DestroyImmediate(child.gameObject);
            swapped++;
        }

        if (swapped > 0)
        {
            Debug.Log($"[TileDemoDistrict] {swapped} Gebaeude in '{copy.name}' gegen Alternativen getauscht.");
        }
        else
        {
            Debug.LogWarning($"[TileDemoDistrict] In '{copy.name}' wurde kein Kind-Objekt gefunden, das '{buildingNameFilter}' im Namen enthaelt -- " +
                              "'Building Name Filter' im Inspector anpassen (schau in der Hierarchy nach den echten Objektnamen).");
        }
    }

    private Bounds MeasureBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[TileDemoDistrict] Keine Renderer im Source District gefunden.");
            return new Bounds(root.position, Vector3.zero);
        }
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    private void PlaceGameplayMarkers(List<Vector3> districtCenters, float districtSizeX, float districtSizeZ)
    {
        Transform markerRoot = new GameObject("Gameplay_Markers").transform;
        markerRoot.SetParent(mapRoot, true);

        for (int i = 0; i < clueCount && i < districtCenters.Count; i++)
        {
            SpawnMarker(clueSpawnMarkerPrefab, districtCenters[i], markerRoot, $"ClueSpawn_{i}");
        }

        Vector3 mapCenter = Vector3.zero;
        foreach (var c in districtCenters) mapCenter += c;
        mapCenter /= Mathf.Max(1, districtCenters.Count);
        SpawnMarker(bossSpawnMarkerPrefab, mapCenter, markerRoot, "BossSpawn");

        int lootIndex = 0;
        foreach (var center in districtCenters)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = center + new Vector3(
                    Random.Range(-districtSizeX * 0.3f, districtSizeX * 0.3f),
                    0,
                    Random.Range(-districtSizeZ * 0.3f, districtSizeZ * 0.3f));
                SpawnMarker(lootSpawnMarkerPrefab, pos, markerRoot, $"LootSpawn_{lootIndex}");
                lootIndex++;
            }
        }

        int spawnsPerDistrict = Mathf.CeilToInt((float)playerSpawnCount / districtCenters.Count);
        int spawnIndex = 0;
        foreach (var center in districtCenters)
        {
            for (int i = 0; i < spawnsPerDistrict && spawnIndex < playerSpawnCount; i++)
            {
                float angle = (360f / spawnsPerDistrict) * i;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * (Mathf.Min(districtSizeX, districtSizeZ) * 0.45f);
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
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }
    }
}
