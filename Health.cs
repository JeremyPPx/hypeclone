using UnityEngine;

// Einfache Health-Komponente fuer Test-Ziele. Bei 0 HP wird das Objekt deaktiviert
// (kein echtes Ragdoll/Death-Feedback noetig fuer den Prototyp).
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"{gameObject.name} hat {amount} Schaden bekommen ({currentHealth}/{maxHealth} HP).");
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} ist besiegt.");
        gameObject.SetActive(false);
    }
}
