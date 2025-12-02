using UnityEngine;

public class WideAttackPowerUp : PoweUpBase
{
    [SerializeField] private float multiplier = 2f;

    protected override void ApplyPowerUp(GameObject target)
    {
        var playerStats = target.GetComponent<PlayerAttack>();
        if (playerStats != null)
        {
            playerStats.MyTargetDetection.AddRangeMultiplier(multiplier);
            Debug.Log("Wide Attack Power-Up applied with multiplier: " + multiplier);
        }
    }

}
