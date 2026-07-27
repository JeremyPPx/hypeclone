using System.Collections.Generic;
using UnityEngine;

// Generiert eine Karte aus 4 Stadtteilen (2x2-Raster) aus deinen eigenen Gebaeude-Prefabs.
// Kein Runtime-Zufall im Spiel selbst -- das hier ist ein EDITOR-Werkzeug: einmal Werte
// im Inspector eintragen, per Rechtsklick auf die Komponente -> "Generate Map" ausloesen,
// Ergebnis von Hand nachbessern, fertig. Erzeugt zusaetzlich Marker fuer Loot/Hinweise/
// Boss/Spieler-Spawns, damit der Bounty-Hunt-Modus direkt darauf aufbauen kann.
//
// Wichtig: die eigentliche Begehbarkeit von Innenraeumen haengt davon ab, ob deine
// Gebaeude-Prefabs selbst Innenraum-Geometrie + Collider mitbringen. Dieses Script
// platziert nur ganze Prefabs, es erzeugt keine Innenraeume.
public class DistrictMapGenerator : MonoBehaviour
{
    [System.Serializable]
    public class BuildingTier
    {
        public string name = "Tier";
        public GameObject[] prefabs;
        [Tooltip("Ungefaehre Dachhoehe dieser Gebaeude-Tier, fuer Sprung-Abstandsberechnung.")]
        public float approxRoofHeight = 10f;
    }

    [Header("Gebaeude (nach Groesse/Hoehe sortiert)")]
    public BuildingTier[] buildingTiers;

    [Header("Strassen")]
    public GameObject roadSegmentPrefab;
    public float roadWidth = 8f;

    [Header("Layout")]
    public int districtsPerSide = 2; // 2x2 = 4 Stadtteile
    public float districtSize = 120f;
    public int plotsPerDistrictSide = 4; // wie viele Gebaeude-Plots pro Stadtteil-Kante
    public float plotPadding = 2f;

    [Header("Verticality (Hyper-Scape-Feeling)")]
    [Tooltip("Maximaler Sprung-/Dash-Abstand zwischen Daechern, damit Movement/Hacks das ueberbruecken koennen. An HackSystem.dashDistance + PlayerMovement.jumpHeight orientieren.")]
    public float maxRoofGap = 7f;

    [Header("Marker-Prefabs (leer lassen = einfacher Platzhalter-Wuerfel)")]
    public GameObject lootSpawnMarkerPrefab;
    public GameObject clueSpawnMarkerPrefab;
    public GameObject bossSpawnMarkerPrefab;
    public GameObject playerSpawnMarkerPrefab;

    [Header("Bounty-Hunt-Vorgaben")]
    public int clueCount = 3;
    public int playerSpawnCount = 8;

    private Transform mapRoot;

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ClearExisting();

        mapRoot = new GameObject("Generated_Map").transform;
        mapRoot.SetParent(transform, false);

        var districtCenters = new List<Vector3>();

        for (int dz = 0; dz < districtsPerSide; dz++)
        {
            for (int dx = 0; dx < districtsPerSide; dx++)
            {
                Vector3 districtOrigin = new Vector3(dx * districtSize, 0, dz * districtSize);
                Transform districtRoot = new GameObject($"District_{dx}_{dz}").transform;
                districtRoot.SetParent(mapRoot, false);
                districtRoot.position = districtOrigin;

                GenerateDistrict(districtRoot, districtOrigin);
                districtCenters.Add(districtOrigin + new Vector3(districtSize * 0.5f, 0, districtSize * 0.5f));
            }
        }

        PlaceGameplayMarkers(districtCenters);

        Debug.Log($"[DistrictMapGenerator] Karte generiert: {districtsPerSide * districtsPerSide} Stadtteile, " +
                  $"{clueCount} Hinweise, 1 Boss-Punkt, {playerSpawnCount} Spieler-Spawns.");
    }

    private void GenerateDistrict(Transform districtRoot, Vector3 origin)
    {
        float plotSize = (districtSize - roadWidth) / plotsPerDistrictSide;

        for (int z = 0; z < plotsPerDistrictSide; z++)
        {
            for (int x = 0; x < plotsPerDistrictSide; x++)
            {
                Vector3 plotCenter = origin + new Vector3(
                    x * plotSize + plotSize * 0.5f + roadWidth * 0.5f,
                    0,
                    z * plotSize + plotSize * 0.5f + roadWidth * 0.5f
                );

                // Randplaetze (aussen im Stadtteil) bekommen eher niedrige Gebaeude,
                // damit Daecher untereinander im Sprung-Abstand (maxRoofGap) erreichbar bleiben.
                bool isEdgePlot = x == 0 || z == 0 || x == plotsPerDistrictSide - 1 || z == plotsPerDistrictSide - 1;
                BuildingTier tier = PickTier(isEdgePlot);
                if (tier == null || tier.prefabs == null || tier.prefabs.Length == 0) continue;

                GameObject prefab = tier.prefabs[Random.Range(0, tier.prefabs.Length)];
                float rotationY = Random.Range(0, 4) * 90f;

                GameObject instance = Instantiate(prefab, plotCenter, Quaternion.Euler(0, rotationY, 0), districtRoot);
                instance.name = $"{prefab.name}_{x}_{z}";
            }
        }

        // Einfaches Strassenraster an den Kanten des Stadtteils (Platzhalter falls kein Prefab gesetzt).
        if (roadSegmentPrefab != null)
        {
            PlaceRoadRing(districtRoot, origin);
        }
    }

    private BuildingTier PickTier(bool isEdgePlot)
    {
        if (buildingTiers == null || buildingTiers.Length == 0) return null;

        if (isEdgePlot)
        {
            // Niedrigste verfuegbare Tier bevorzugen (sortiert nach approxRoofHeight).
            BuildingTier lowest = buildingTiers[0];
            foreach (var t in buildingTiers)
            {
                if (t.approxRoofHeight < lowest.approxRoofHeight) lowest = t;
            }
            return lowest;
        }

        return buildingTiers[Random.Range(0, buildingTiers.Length)];
    }

    private void PlaceRoadRing(Transform districtRoot, Vector3 origin)
    {
        Vector3 center = origin + new Vector3(districtSize * 0.5f, 0, districtSize * 0.5f);
        GameObject road = Instantiate(roadSegmentPrefab, center, Quaternion.identity, districtRoot);
        road.name = "RoadSegment";
        road.transform.localScale = new Vector3(districtSize, 1, roadWidth);
    }

    private void PlaceGameplayMarkers(List<Vector3> districtCenters)
    {
        Transform markerRoot = new GameObject("Gameplay_Markers").transform;
        markerRoot.SetParent(mapRoot, false);

        // 3 Hinweise: auf 3 der 4 Stadtteile verteilt (deterministisch, damit reproduzierbar).
        for (int i = 0; i < clueCount && i < districtCenters.Count; i++)
        {
            SpawnMarker(clueSpawnMarkerPrefab, districtCenters[i] + Random.insideUnitSphere * 10f, markerRoot, $"ClueSpawn_{i}");
        }

        // Boss-Punkt: Mittelpunkt der gesamten Karte.
        Vector3 mapCenter = Vector3.zero;
        foreach (var c in districtCenters) mapCenter += c;
        mapCenter /= Mathf.Max(1, districtCenters.Count);
        SpawnMarker(bossSpawnMarkerPrefab, mapCenter, markerRoot, "BossSpawn");

        // Feste Loot-Spawns: 2 pro Stadtteil.
        int lootIndex = 0;
        foreach (var center in districtCenters)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = center + Random.insideUnitSphere * (districtSize * 0.3f);
                pos.y = 0;
                SpawnMarker(lootSpawnMarkerPrefab, pos, markerRoot, $"LootSpawn_{lootIndex}");
                lootIndex++;
            }
        }

        // Spieler-Spawns: gleichmaessig auf die Stadtteil-Raender verteilt (fair fuer alle 8).
        int spawnsPerDistrict = Mathf.CeilToInt((float)playerSpawnCount / districtCenters.Count);
        int spawnIndex = 0;
        foreach (var center in districtCenters)
        {
            for (int i = 0; i < spawnsPerDistrict && spawnIndex < playerSpawnCount; i++)
            {
                float angle = (360f / spawnsPerDistrict) * i;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * (districtSize * 0.45f);
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
            // Platzhalter, falls noch kein eigenes Marker-Prefab zugewiesen ist.
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
