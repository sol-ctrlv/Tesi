using System.Collections;
using UnityEngine;

/// <summary>
/// Gestisce i buff temporanei dati dalla chest:
/// - attacco più ampio
/// - attacco più forte
/// - scudo che annulla il prossimo danno
/// </summary>
public class PlayerBuffReceiver : MonoBehaviour
{
    [Header("Valori base (li usa il tuo sistema di attacco/danno)")]
    public float baseAttackRange = 1f;
    public float baseAttackDamage = 1f;

    public float AttackRangeMultiplier { get; private set; } = 1f;
    public float AttackDamageMultiplier { get; private set; } = 1f;

    public bool HasShieldOnce { get; private set; } = false;

    private Coroutine rangeBuffRoutine;
    private Coroutine damageBuffRoutine;

    /// <summary>
    /// Chiamato dalla chest quando assegna un buff.
    /// </summary>
    public void ApplyRewardBuff(RewardBuffType buffType, float magnitude, float duration)
    {
        switch (buffType)
        {
            case RewardBuffType.WiderAttack:
                ApplyRangeBuff(magnitude, duration);
                break;

            case RewardBuffType.StrongerAttack:
                ApplyDamageBuff(magnitude, duration);
                break;

            case RewardBuffType.ShieldOnce:
                ApplyShieldOnce();
                break;
        }
    }

    private void ApplyRangeBuff(float magnitude, float duration)
    {
        if (rangeBuffRoutine != null)
            StopCoroutine(rangeBuffRoutine);

        rangeBuffRoutine = StartCoroutine(RangeBuffCoroutine(magnitude, duration));
    }

    private IEnumerator RangeBuffCoroutine(float magnitude, float duration)
    {
        AttackRangeMultiplier = magnitude;
        Debug.Log($"[PlayerBuffReceiver] Buff range x{magnitude} per {duration} s");

        yield return new WaitForSeconds(duration);

        AttackRangeMultiplier = 1f;
        rangeBuffRoutine = null;
        Debug.Log("[PlayerBuffReceiver] Buff range finito");
    }

    private void ApplyDamageBuff(float magnitude, float duration)
    {
        if (damageBuffRoutine != null)
            StopCoroutine(damageBuffRoutine);

        damageBuffRoutine = StartCoroutine(DamageBuffCoroutine(magnitude, duration));
    }

    private IEnumerator DamageBuffCoroutine(float magnitude, float duration)
    {
        AttackDamageMultiplier = magnitude;
        Debug.Log($"[PlayerBuffReceiver] Buff damage x{magnitude} per {duration} s");

        yield return new WaitForSeconds(duration);

        AttackDamageMultiplier = 1f;
        damageBuffRoutine = null;
        Debug.Log("[PlayerBuffReceiver] Buff damage finito");
    }

    private void ApplyShieldOnce()
    {
        HasShieldOnce = true;
        Debug.Log("[PlayerBuffReceiver] Scudo: il prossimo danno sarà annullato");
    }

    /// <summary>
    /// Da chiamare nel tuo sistema di danno PRIMA di togliere HP.
    /// Ritorna true se il danno è stato annullato.
    /// </summary>
    public bool TryConsumeShield()
    {
        if (!HasShieldOnce)
            return false;

        HasShieldOnce = false;
        Debug.Log("[PlayerBuffReceiver] Scudo consumato, danno annullato");
        return true;
    }
}
