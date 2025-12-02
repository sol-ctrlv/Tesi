using UnityEngine;

public class StrengthPowerUp : PoweUpBase
{
    [SerializeField] private float multiplier = 2f;

    protected override void ApplyPowerUp(GameObject target)
    {
        var playerStats = target.GetComponent<PlayerAttack>();
        if (playerStats != null)
        {
            playerStats.DamageMultiplier = multiplier;
            Debug.Log("Strength Power-Up applied with multiplier: " + multiplier);
        }
    }

}
