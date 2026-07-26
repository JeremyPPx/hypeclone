using UnityEngine;

// Minimalistisches Hitscan-Gunplay fuers Prototyping: Linksklick = Raycast von der
// Bildschirmmitte, trifft er ein Objekt mit Health-Komponente, gibt's Schaden.
public class SimpleShooter : MonoBehaviour
{
    public Camera playerCamera;
    public float range = 100f;
    public float damage = 20f;
    public float fireRate = 0.2f; // Sekunden zwischen Schuessen

    private float nextFireTime = 0f;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (playerCamera == null) return;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.yellow, 0.2f);
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}
