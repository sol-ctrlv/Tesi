using UnityEngine;

public class SnowmanAnimToActor : MonoBehaviour
{
    [SerializeField] SnowmanFireTimer snowmanFireTimer;

    public void ThrowSnowball()
    {
        snowmanFireTimer.PlayAttackAnimation();
    }
}
