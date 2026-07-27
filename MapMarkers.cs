using UnityEngine;

// Winzige Marker-Komponenten, damit die spaetere Bounty-Hunt-Spiellogik die vom
// DistrictMapGenerator erzeugten Punkte einfach per FindObjectsOfType<...>() finden kann,
// statt sich auf Namen/Tags verlassen zu muessen. Als Marker-Prefabs im Generator
// zuweisen (z.B. ein simples leeres GameObject mit einer dieser Komponenten drauf).

public class ClueSpawnPoint : MonoBehaviour
{
    public bool found = false;
}

public class LootSpawnPoint : MonoBehaviour
{
    public GameObject[] possibleLoot;
}

public class BossSpawnPoint : MonoBehaviour
{
}

public class PlayerSpawnPoint : MonoBehaviour
{
}
