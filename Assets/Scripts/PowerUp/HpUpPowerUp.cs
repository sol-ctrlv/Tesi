using UnityEngine;

public class HpUpPowerUp : PoweUpBase
{
    protected override void ApplyPowerUp(GameObject target)
    {
        var playerRef = target.GetComponent<Player>();
        if (playerRef != null)
        {
            playerRef.AddMaxHealth();
            Debug.Log("HP UP Power-Up applied");
        }
    }

}
