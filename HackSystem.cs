using System.Collections;
using UnityEngine;

// Alle 4 Start-Hacks in einem Script gebündelt (fürs Prototyping schneller iterierbar
// als 4 einzelne Komponenten). Spaeter, wenn ein Loot/Inventar-System dazukommt,
// laesst sich das leicht in einzelne Hack-Klassen aufsplitten.
[RequireComponent(typeof(PlayerMovement))]
public class HackSystem : MonoBehaviour
{
    [Header("Dash")]
    public KeyCode dashKey = KeyCode.Q;
    public float dashDistance = 6f;
    public float dashCooldown = 3f;

    [Header("Slam")]
    public KeyCode slamKey = KeyCode.E;
    public float slamRadius = 4f;
    public float slamForce = 15f;
    public LayerMask enemyLayer;

    [Header("Invisibility")]
    public KeyCode invisKey = KeyCode.F;
    public float invisDuration = 3f;
    public float invisCooldown = 8f;

    [Header("Wallhack-Ping")]
    public KeyCode pingKey = KeyCode.R;
    public float pingRadius = 20f;
    public float pingCooldown = 6f;
    public string enemyTag = "Enemy";

    private PlayerMovement movement;
    private Renderer[] renderers;
    private bool dashReady = true, invisReady = true, pingReady = true;
    private bool isSlamming = false;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(dashKey) && dashReady) StartCoroutine(DoDash());
        if (Input.GetKeyDown(slamKey) && !movement.IsGrounded() && !isSlamming) StartCoroutine(DoSlam());
        if (Input.GetKeyDown(invisKey) && invisReady) StartCoroutine(DoInvisibility());
        if (Input.GetKeyDown(pingKey) && pingReady) DoPing();
    }

    private IEnumerator DoDash()
    {
        dashReady = false;
        Vector3 dashMove = transform.forward * dashDistance;
        // Auf mehrere Frames verteilen, damit der CharacterController sauber kollidiert.
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            movement.ApplyExternalMove(dashMove * (Time.deltaTime / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(dashCooldown);
        dashReady = true;
    }

    private IEnumerator DoSlam()
    {
        isSlamming = true;
        movement.ApplyExternalMove(Vector3.down * 0.1f); // kleiner Kick, Fall-Beschleunigung uebernimmt der Rest über die Movement-Gravity
        // Warten bis Spieler wieder am Boden ist
        yield return new WaitUntil(() => movement.IsGrounded());

        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius, enemyLayer);
        foreach (var hit in hits)
        {
            Health health = hit.GetComponent<Health>();
            if (health != null) health.TakeDamage(slamForce);
        }
        isSlamming = false;
    }

    private IEnumerator DoInvisibility()
    {
        invisReady = false;
        SetRenderersVisible(false);
        yield return new WaitForSeconds(invisDuration);
        SetRenderersVisible(true);
        yield return new WaitForSeconds(invisCooldown);
        invisReady = true;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
    }

    private void DoPing()
    {
        pingReady = false;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= pingRadius)
            {
                Debug.Log($"[Ping] Gegner erkannt: {enemy.name} ({dist:F1}m entfernt)");
                Debug.DrawLine(transform.position, enemy.transform.position, Color.red, 2f);
            }
        }
        StartCoroutine(ResetPing());
    }

    private IEnumerator ResetPing()
    {
        yield return new WaitForSeconds(pingCooldown);
        pingReady = true;
    }
}
