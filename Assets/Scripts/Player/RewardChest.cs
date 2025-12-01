using UnityEngine;

/// <summary>
/// Chest che, alla prima interazione del player, assegna
/// uno fra tre buff possibili e poi si disattiva.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RewardChest : MonoBehaviour
{
    [Header("Interazione")]
    [SerializeField] private KeyCode interactKey = KeyCode.X;
    [SerializeField] private bool destroyAfterOpen = false;

    [Header("Buff settings")]
    [SerializeField] private float buffDuration = 30f;
    [SerializeField] private float widerAttackMultiplier = 1.5f;
    [SerializeField] private float strongerAttackMultiplier = 2f;

    private bool playerInRange = false;
    private bool opened = false;
    private GameObject currentPlayer;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        currentPlayer = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (other.gameObject == currentPlayer)
        {
            playerInRange = false;
            currentPlayer = null;
        }
    }

    private void Update()
    {
        if (!playerInRange || opened || currentPlayer == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        opened = true;

        var buffReceiver = currentPlayer.GetComponent<PlayerBuffReceiver>();
        if (buffReceiver != null)
        {
            var buffType = GetRandomBuffType();
            ApplyBuffToPlayer(buffReceiver, buffType);
        }
        else
        {
            Debug.LogWarning("[RewardChest] PlayerBuffReceiver non trovato sul player.");
        }

        // TODO: animazione, suono, cambio sprite…

        if (destroyAfterOpen)
        {
            Destroy(gameObject);
        }
        else
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
        }
    }

    private RewardBuffType GetRandomBuffType()
    {
        int v = Random.Range(0, 3); // 0,1,2
        return (RewardBuffType)v;
    }

    private void ApplyBuffToPlayer(PlayerBuffReceiver receiver, RewardBuffType type)
    {
        switch (type)
        {
            case RewardBuffType.WiderAttack:
                receiver.ApplyRewardBuff(type, widerAttackMultiplier, buffDuration);
                Debug.Log("[RewardChest] Buff assegnato: Attacco più ampio");
                break;

            case RewardBuffType.StrongerAttack:
                receiver.ApplyRewardBuff(type, strongerAttackMultiplier, buffDuration);
                Debug.Log("[RewardChest] Buff assegnato: Attacco più forte");
                break;

            case RewardBuffType.ShieldOnce:
                receiver.ApplyRewardBuff(type, 1f, 0f); // magnitude/duration non usati
                Debug.Log("[RewardChest] Buff assegnato: Scudo (prossimo danno annullato)");
                break;
        }
    }
}
