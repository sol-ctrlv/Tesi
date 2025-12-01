using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SwordPickup : MonoBehaviour
{
    [SerializeField] private bool destroyOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Assumiamo che il player abbia il tag "Player"
        if (!other.CompareTag("Player"))
            return;

        var attack = other.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            attack.UnlockAttack();
        }
        else
        {
            Debug.LogWarning("[SwordPickup] PlayerAttack non trovato sul player.");
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
