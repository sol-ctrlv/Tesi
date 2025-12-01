using UnityEngine;

/// <summary>
/// Cura il player la prima volta che entra nella tile.
/// Dopo l'attivazione può opzionalmente distruggere se stessa.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HealingTile : MonoBehaviour
{
    [SerializeField] private int healAmount = 69;
    [SerializeField] private bool destroyAfterUse = true;

    private bool used = false;

    private void Reset()
    {
        // Se ti dimentichi, si imposta da solo come trigger.
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used)
            return;

        // facciamo triggerare la cura SOLO sul player
        if (!other.CompareTag("Player"))
            return;

        // prendi il componente salute dal player
        var health = other.GetComponent<Player>();
        if (health == null)
        {
            Debug.LogWarning("[OneShotHealingTile] PlayerHealth non trovato sul player.");
            return;
        }

        // applica la cura
        health.Heal(healAmount);

        used = true;

        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
        else
        {
            // in alternativa potresti disattivare solo il collider per lasciare la tile visiva.
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
        }
    }
}
